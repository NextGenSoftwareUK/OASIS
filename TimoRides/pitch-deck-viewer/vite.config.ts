import fs from "node:fs";
import path from "node:path";
import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

const deckPath = path.resolve(
  __dirname,
  "../automation/timo_rides_new_deck.schema.yaml",
);

const deckSchemaPlugin = () => {
  return {
    name: "timo-deck-schema",
    enforce: "pre",
    resolveId(source: string, importer: string | undefined) {
      if (source === "#deck-schema") {
        return "#deck-schema";
      }
      if (source === "#deck-schema?raw") {
        return "#deck-schema?raw";
      }
      if (source === "#deck-schema?url") {
        return "#deck-schema?url";
      }
      if (source.startsWith("#deck-schema?")) {
        // preserve query for Vite to attach via ?import
        const [, query] = source.split("?");
        return `#deck-schema?${query}`;
      }
      return null;
    },
    load(id: string) {
      if (id.startsWith("#deck-schema")) {
        const contents = fs.readFileSync(deckPath, "utf-8");

        if (id === "#deck-schema" || id === "#deck-schema?import") {
          return `export default ${JSON.stringify(contents)};`;
        }

        if (id === "#deck-schema?url") {
          return `const blob = new Blob([${JSON.stringify(
            contents,
          )}], { type: 'text/plain' });
export default URL.createObjectURL(blob);`;
        }

        if (id === "#deck-schema?raw") {
          return `export default ${JSON.stringify(contents)};`;
        }

        // handle generic queries like ?raw&inline
        return `export default ${JSON.stringify(contents)};`;
      }
      return null;
    },
    handleHotUpdate(ctx: any) {
      if (ctx.file === deckPath) {
        ctx.server.ws.send({
          type: "full-reload",
        });
      }
    },
  };
};

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), deckSchemaPlugin()],
  server: {
    fs: {
      allow: [path.resolve(__dirname, "..")],
    },
  },
});
