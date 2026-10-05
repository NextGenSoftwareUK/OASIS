import { useEffect, useState } from "react";
import { FileRejection, useDropzone } from "react-dropzone";
import { Loader2 } from "lucide-react";
import {
  FormField,
  FormItem,
  FormControl,
  FormMessage,
} from "@/components/ui/form";
import { handleUploadFile } from "@/utils/handleUploadFile.util";
import { Control, useFormContext } from "react-hook-form";
import { MAX_FILE_SIZE } from "@/lib/constants";

interface DragAndDropProps {
  control: Control<any>;
  name: string;
  isSuccessfullyDone: boolean;
  className?: string;
}

export function DragAndDropUpload({
  control,
  name,
  isSuccessfullyDone,
  className,
}: DragAndDropProps) {
  const [preview, setPreview] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);

  const { setError, clearErrors } = useFormContext();

  const onDrop = async (acceptedFiles: File[]) => {
    const file = acceptedFiles[0];
    if (!file) return;

    setIsUploading(true);
    clearErrors(name);

    try {
      const uploadedUrl = await handleUploadFile(file, "NFT-Logo");
      if (uploadedUrl.includes("http")) {
        setPreview(URL.createObjectURL(file));
        formField.onChange(uploadedUrl);
      } else {
        throw new Error("Invalid upload URL");
      }
    } catch (err: any) {
      setError(name, {
        type: "manual",
        message: err.message || "Upload failed",
      });
    } finally {
      setIsUploading(false);
    }
  };

  const onDropRejected = (fileRejections: FileRejection[]) => {
    const rejection = fileRejections[0];
    const error = rejection.errors[0];

    if (error.code === "file-too-large") {
      setError(name, {
        type: "manual",
        message: "File must be smaller than 100MB",
      });
    } else {
      setError(name, { type: "manual", message: error.message });
    }
  };

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    onDropRejected,
    maxSize: MAX_FILE_SIZE,
    accept: { "image/*": [], "application/pdf": [] },
    multiple: false,
  });

  useEffect(() => {
    if (isSuccessfullyDone) {
      setPreview(null);
    }
  }, [isSuccessfullyDone]);

  return (
    <div className={className}>
      <FormField
        control={control}
        name={name}
        render={({ field }) => {
          formField = field;
          return (
            <FormItem>
              <FormControl>
                <div className="aspect-square h-auto rounded-2xl bg-text-gray">
                  <div
                    {...getRootProps({
                      className: `flex justify-center items-center border-2 border-dashed border-gray p-4 rounded-md text-center cursor-pointer h-full transition-colors duration-150 ${
                        isUploading
                          ? "bg-gray-700 cursor-not-allowed opacity-70"
                          : ""
                      }`,
                    })}
                  >
                    <input {...getInputProps()} />
                    {isUploading ? (
                      <div className="flex flex-col items-center gap-2 text-white">
                        <Loader2 className="animate-spin" size={32} />
                        <p className="p">Uploading...</p>
                      </div>
                    ) : preview || field.value ? (
                      <img
                        src={preview || field.value}
                        alt="Preview"
                        className="mx-auto max-h-64 rounded-md object-contain"
                      />
                    ) : (
                      <div className="flex flex-col gap-3 justify-center text-white">
                        <h2 className="h1">RWA Image</h2>
                        <p className="p">
                          {isDragActive
                            ? "Drop the file here..."
                            : "Drag & drop or click to upload an image"}
                        </p>
                      </div>
                    )}
                  </div>
                </div>
              </FormControl>
              <FormMessage />
            </FormItem>
          );
        }}
      />
    </div>
  );
}

let formField: any;
