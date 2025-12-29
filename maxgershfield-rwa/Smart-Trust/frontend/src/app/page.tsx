import Link from "next/link";

export default function Home() {
  return (
    <div className="min-h-screen bg-gray-900 flex items-center justify-center">
      <div className="text-center">
        <h1 className="text-4xl font-bold text-white mb-8">
          Smart Trust Wizard
        </h1>
        <p className="text-gray-300 mb-8">
          Create Wyoming Smart Trusts with our guided wizard
        </p>
        <Link 
          href="/trust/create"
          className="bg-blue-600 hover:bg-blue-700 text-white font-bold py-3 px-6 rounded-lg transition-colors"
        >
          Start Trust Creation
        </Link>
      </div>
    </div>
  );
}

