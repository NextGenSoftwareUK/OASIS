import { uploadFile } from "@/requests/file/uploadFile.request";

export const handleUploadFile = async (file: File, type: "NFT-Logo" | "Document" = "Document"): Promise<string> => {
  try {
    const res = await uploadFile(file, type);
    // Handle both the expected nested structure and our mock API structure
    const fileUrl = (res.data as any)?.data?.fileUrl || (res.data as any)?.fileUrl;
    if (fileUrl && fileUrl.includes("http")) {
      return fileUrl;
    } else {
      throw new Error("Invalid response structure");
    }
  } catch (error) {
    console.error("Upload failed", error);
    throw new Error("File upload failed. Please try again.");
  }
};
