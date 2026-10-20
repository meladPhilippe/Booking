import "./styles.css";

const apiOrigin = (import.meta.env.VITE_PAYMENT_API_ORIGIN || "https://localhost:5001").replace(/\/$/, "");
const statusNames = ["Pending", "Processing", "Requires action", "Succeeded", "Failed", "Cancelled", "Requires reconciliation"];
const terminalStatuses = new Set([3, 4, 5, 6]);
const state = { paymentId: sessionStorage.getItem("stripeSandboxPaymentId"), checkout: null, poll: null };

const notice = document.querySelector("#notice");
const paymentDetails = document.querySelector("#payment-details");
const refundPanel = document.querySelector("#refund-panel");
const paymentForm = document.querySelector("#payment-form");
const refundForm = document.querySelector("#refund-form");

function apiUrl(path) {
  return `${apiOrigin}${path}`;
}

function setNotice(message, isError = false) {
  notice.textContent = message;
  notice.className = isError ? "error" : "";
}

async function request(path, options = {}) {
  const response = await fetch(apiUrl(path), options);
  if (response.ok) return response.status === 204 ? null : response.json();
  const problem = await response.json().catch(() => ({}));
  throw new Error(problem.title || problem.detail || `Request failed (${response.status}).`);
}

function renderPayment(payment) {
  paymentDetails.classList.remove("hidden");
  document.querySelector("#payment-id").textContent = payment.paymentId;
  document.querySelector("#provider-reference").textContent = payment.providerReference || "Not available yet";
  document.querySelector("#payment-status").textContent = statusNames[payment.status] || `Unknown (${payment.status})`;
  document.querySelector("#refund-amount").max = payment.amount;
  if (payment.status === 3) {
    refundPanel.classList.remove("hidden");
    setNotice("Payment succeeded. You can now create a full or partial refund.");
  } else if (terminalStatuses.has(payment.status)) {
    setNotice(`Payment finished with status: ${statusNames[payment.status]}.`, payment.status !== 3);
  } else {
    setNotice("Waiting for Stripe's verified webhook confirmation…");
  }
}

async function pollPayment() {
  if (!state.paymentId) return;
  try {
    const payment = await request(`/api/v1/payments/${state.paymentId}`);
    renderPayment(payment);
    if (terminalStatuses.has(payment.status)) {
      clearInterval(state.poll);
      state.poll = null;
    }
  } catch (error) {
    setNotice(error.message, true);
  }
}

function startPolling() {
  clearInterval(state.poll);
  pollPayment();
  state.poll = setInterval(pollPayment, 2000);
}

paymentForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  const button = document.querySelector("#create-payment");
  button.disabled = true;
  refundPanel.classList.add("hidden");
  try {
    const payment = await request("/api/v1/payments", {
      method: "POST",
      headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({
        merchantReference: document.querySelector("#merchant-reference").value,
        amount: Number(document.querySelector("#amount").value),
        currency: document.querySelector("#currency").value,
        paymentMethod: "Card",
        returnUrl: window.location.origin,
        cancelUrl: window.location.origin
      })
    });
    state.paymentId = payment.paymentId;
    sessionStorage.setItem("stripeSandboxPaymentId", state.paymentId);
    renderPayment(payment);
    if (payment.nextAction?.type !== "embedded_checkout" || !payment.nextAction.data?.clientSecret)
      throw new Error("The selected provider did not return an embedded Checkout action.");
    state.checkout?.destroy();
    state.checkout = await Stripe(window.stripePublishableKey).initEmbeddedCheckout({ clientSecret: payment.nextAction.data.clientSecret });
    state.checkout.mount("#checkout");
    startPolling();
  } catch (error) {
    setNotice(error.message, true);
  } finally {
    button.disabled = false;
  }
});

refundForm.addEventListener("submit", async (event) => {
  event.preventDefault();
  try {
    const refund = await request(`/api/v1/payments/${state.paymentId}/refunds`, {
      method: "POST",
      headers: { "Content-Type": "application/json", "Idempotency-Key": crypto.randomUUID() },
      body: JSON.stringify({ amount: Number(document.querySelector("#refund-amount").value) })
    });
    document.querySelector("#refund-status").textContent = `Refund ${refund.refundId}: ${refund.status}.`;
  } catch (error) {
    document.querySelector("#refund-status").textContent = error.message;
  }
});

(async () => {
  try {
    const config = await request("/api/v1/client-config/stripe");
    window.stripePublishableKey = config.publishableKey;
    setNotice("Ready. Create a payment to mount Stripe Embedded Checkout.");
    if (state.paymentId) startPolling();
  } catch (error) {
    const reason = error instanceof Error ? error.message : "Unable to reach the Payment API.";
    setNotice(`Stripe sandbox is unavailable: ${reason}`, true);
    document.querySelector("#create-payment").disabled = true;
  }
})();
