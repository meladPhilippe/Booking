# STEP 1 — Start with a small Node.js image; Vite needs Node to build and serve
# this browser-only development demo.
FROM node:22-alpine

WORKDIR /app

# STEP 2 — Copy dependency definitions first so Docker can reuse this layer
# unless package.json/package-lock.json changes.
COPY package*.json ./
RUN npm ci

# STEP 3 — Copy the HTML, JavaScript, CSS, and Vite configuration.
COPY . ./

EXPOSE 5173

# STEP 4 — Run Vite's local development server, not a production storefront.
CMD ["npm", "run", "dev"]
