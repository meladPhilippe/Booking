// STEP 0 — Load the visual styling for this small teaching/demo application.
import "./styles.css";

// STEP 1 — Identify the Payment API. In Docker this value is supplied as an
// environment setting; when working locally it falls back to port 5001.
const apiOrigin = (import.meta.env.VITE_PAYMENT_API_ORIGIN || "https://localhost:5001").replace(/\/$/, "");

// STEP 2 — Translate the API's numeric payment status into words people can read.
const statusNames = ["Pending", "Processing", "Requires action", "Succeeded", "Failed", "Cancelled", "Requires reconciliation"];

// These are final states. Once we see one, there is no reason to keep asking
// the API for updates every two seconds.
const terminalStatuses = new Set([3, 4, 5, 6]);

// STEP 3 — Keep only temporary browser state. The payment ID is saved for this
// browser tab so a refresh can continue showing its status. We never save the
// Checkout client secret or Stripe account credentials in browser storage.
const state = { paymentId: sessionStorage.getItem("stripeSandboxPaymentId"), checkout: null, poll: null };

// STEP 4 — Find the named areas of the HTML page that JavaScript will update.
const notice = document.querySelector("#notice");
const paymentDetails = document.querySelector("#payment-details");
const refundPanel = document.querySelector("#refund-panel");
const paymentForm = document.querySelector("#payment-form");
const refundForm = document.querySelector("#refund-form");

function apiUrl(path) {
  // Turn a relative API path into a complete address, for example
  // https://localhost:5001/api/v1/payments.
  return `${apiOrigin}${path}`;
}

function setNotice(message, isError = false) {
  // Put a human-readable progress/error message at the top of the page.
  notice.textContent = message;
  notice.className = isError ? "error" : "";
}

async function request(path, options = {}) {
  // One shared helper for talking to the Payment API. It returns JSON for a
  // successful answer and turns API error responses into readable messages.
  const response = await fetch(apiUrl(path), options);
  if (response.ok) return response.status === 204 ? null : response.json();
  const problem = await response.json().catch(() => ({}));
  throw new Error(problem.title || problem.detail || `Request failed (${response.status}).`);
}

function renderPayment(payment) {
  // STEP 5 — Show the latest payment record that came from our Payment API.
  // This record is the application's source of truth, not the Checkout form.
  paymentDetails.classList.remove("hidden");
  document.querySelector("#payment-id").textContent = payment.paymentId;
  document.querySelector("#provider-reference").textContent = payment.providerReference || "Not available yet";
  document.querySelector("#payment-status").textContent = statusNames[payment.status] || `Unknown (${payment.status})`;
  document.querySelector("#payment-updated-at").textContent = new Date(payment.updatedAt).toLocaleString();
  document.querySelector("#refund-amount").max = payment.amount;
  if (payment.status === 3) {
    // A succeeded status was written after a verified Stripe webhook, so a
    // refund is now meaningful in this sandbox demo.
    refundPanel.classList.remove("hidden");
    document.querySelector("#webhook-status").textContent = "Verified Stripe webhook confirmed payment success.";
    setNotice("Payment succeeded after verified Stripe webhook confirmation. You can now create a full or partial refund.");
  } else if (terminalStatuses.has(payment.status)) {
    // Failed/cancelled/reconciliation-needed states are final for the browser.
    document.querySelector("#webhook-status").textContent = "A verified Stripe webhook completed the payment lifecycle.";
    setNotice(`Payment finished with status: ${statusNames[payment.status]}.`, payment.status !== 3);
  } else {
    // Stripe has not yet sent, or the API has not yet processed, its verified
    // webhook. The browser waits by polling a safe read endpoint.
    document.querySelector("#webhook-status").textContent = "Waiting for Stripe to deliver and the API to verify a webhook. This page polls the payment API; it never calls the webhook endpoint.";
    setNotice("Waiting for Stripe's verified webhook confirmation…");
  }
}

async function pollPayment() {
  // STEP 6 — Ask the Payment API for the current local payment status. The
  // browser never calls Stripe's webhook endpoint; Stripe calls that endpoint.
  if (!state.paymentId) return;
  try {
    const payment = await request(`/api/v1/payments/${state.paymentId}`);
    renderPayment(payment);
    if (terminalStatuses.has(payment.status)) {
      // Stop the repeating timer as soon as the lifecycle reaches a final state.
      clearInterval(state.poll);
      state.poll = null;
    }
  } catch (error) {
    setNotice(error.message, true);
  }
}

function startPolling() {
  // Start one immediate status check, then repeat every two seconds. Clearing
  // the previous timer prevents duplicate polling after a page interaction.
  clearInterval(state.poll);
  pollPayment();
  state.poll = setInterval(pollPayment, 2000);
}

paymentForm.addEventListener("submit", async (event) => {
  // STEP 7 — The customer clicked “Create secure checkout”. Prevent a normal
  // browser form submission because JavaScript will call the Payment API.
  event.preventDefault();
  const button = document.querySelector("#create-payment");
  button.disabled = true;
  refundPanel.classList.add("hidden");
  try {
    // Ask our API to create a payment. The idempotency key means a retry of the
    // same request cannot accidentally create a second payment.
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
    // Remember only the module payment ID for this browser tab; do not store
    // Stripe secrets here.
    sessionStorage.setItem("stripeSandboxPaymentId", state.paymentId);
    renderPayment(payment);
    if (payment.nextAction?.type !== "embedded_checkout" || !payment.nextAction.data?.clientSecret)
      throw new Error("The selected provider did not return an embedded Checkout action.");

    // STEP 8 — Stripe.js needs two browser-safe values: the publishable key
    // (which identifies the Stripe account) and this Checkout Session's client
    // secret (which opens this one already-created Checkout Session).
    state.checkout?.destroy();
    state.checkout = await Stripe(window.stripePublishableKey).initEmbeddedCheckout({ clientSecret: payment.nextAction.data.clientSecret });
    state.checkout.mount("#checkout");

    // STEP 9 — Start waiting for the webhook-updated status while the customer
    // interacts with Stripe's embedded form.
    startPolling();
  } catch (error) {
    // Show errors without exposing raw server details or credentials.
    setNotice(error.message, true);
  } finally {
    // Re-enable the button even if creation failed so the user can correct input.
    button.disabled = false;
  }
});

refundForm.addEventListener("submit", async (event) => {
  // STEP 10 — This optional sandbox action asks the API to refund a verified
  // successful payment. The API—not this browser—calls Stripe's refund API.
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
  // STEP 11 — When the page first opens, get the sandbox publishable key from
  // the development-only API endpoint. This is the only Stripe account key the
  // browser receives. The restricted/secret key stays on the Payment API.
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
