import Modal from "@/components/Modal";
import SignInForm from "@/components/SignInForm";

export default function SignInModal() {
  return (
    <Modal className="relative">
      <h2 className="h2 mb-6 text-black">Sign In</h2>
      <SignInForm />
    </Modal>
  );
}
