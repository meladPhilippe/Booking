import fs from "node:fs";
import { defineConfig } from "vite";

export default defineConfig(({ command }) => {
  if (command !== "serve") return {};

  const certificatePath = process.env.VITE_HTTPS_PFX_PATH;
  const certificatePassword = process.env.VITE_HTTPS_PFX_PASSWORD;
  if (!certificatePath || !certificatePassword)
    throw new Error("VITE_HTTPS_PFX_PATH and VITE_HTTPS_PFX_PASSWORD are required to run the HTTPS demo.");

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
