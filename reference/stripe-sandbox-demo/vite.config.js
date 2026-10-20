// This file configures Vite, the small local web server used by the demo.
import fs from "node:fs";
import { defineConfig } from "vite";

export default defineConfig(({ command }) => {
  // A production-style `vite build` does not need a local HTTPS certificate.
  if (command !== "serve") return {};

  const certificatePath = process.env.VITE_HTTPS_PFX_PATH;
  const certificatePassword = process.env.VITE_HTTPS_PFX_PASSWORD;
  // The browser demo uses HTTPS locally because return URLs and browser security
  // rules should resemble a real checkout integration.
  if (!certificatePath || !certificatePassword)
    throw new Error("VITE_HTTPS_PFX_PATH and VITE_HTTPS_PFX_PASSWORD are required to run the HTTPS demo.");

  // Bind inside the Docker container and expose one predictable HTTPS port.
  return {
    server: {
      host: "0.0.0.0",
      port: 5173,
      strictPort: true,
      https: {
        pfx: fs.readFileSync(certificatePath),
        passphrase: certificatePassword
      }
    }
  };
});
