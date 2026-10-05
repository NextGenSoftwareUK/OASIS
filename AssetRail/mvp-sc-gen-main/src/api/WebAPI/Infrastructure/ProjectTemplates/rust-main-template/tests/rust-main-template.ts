import * as anchor from "@project-serum/anchor";
import { Program } from "@project-serum/anchor";
import { RustMainTemplate } from "../target/types/rust_main_template";

describe("rust-main-template", () => {
  // Configure the client to use the local cluster.
  anchor.setProvider(anchor.AnchorProvider.env());

  const program = anchor.workspace.RustMainTemplate as Program<RustMainTemplate>;

  it("Is initialized!", async () => {
    // Add your test here.
    const tx = await program.methods.initialize().rpc();
    console.log("Your transaction signature", tx);
  });
});
