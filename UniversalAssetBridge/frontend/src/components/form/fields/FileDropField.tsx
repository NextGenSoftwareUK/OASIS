"use client";

import { Button } from "@/components/ui/button";
import {
  Dropzone,
  DropzoneContent,
  DropzoneEmptyState,
  DropzoneProps,
  renderBytes,
} from "@/components/ui/shadcn-io/dropzone";
import { showLoadingOnUploading } from "@/utils/showLoadingOnUploading.util";
import { FileIcon, X } from "lucide-react";
import { ChangeEvent, useState } from "react";
import { DropEvent, FileRejection } from "react-dropzone";
import {
  ControllerRenderProps,
  FieldValues,
  UseFormReturn,
} from "react-hook-form";

interface FormProps extends DropzoneProps {
  form: UseFormReturn<any>;
  field: ControllerRenderProps<FieldValues, string>;
}

export default function FileDropField({ form, field, ...props }: FormProps) {
  const [files, setFiles] = useState<File[] | undefined>();
  const [isUploading, setIsUploading] = useState(false);

  const handleDrop = async (
    files: File[],
    _: FileRejection[],
    e: DropEvent
  ) => {
    console.log(files);
    setFiles(files);
    showLoadingOnUploading(
      e as ChangeEvent<HTMLInputElement>,
      form,
      field,
      setIsUploading
    );
  };
  return (
    <>
      <Dropzone
        onDrop={handleDrop}
        onError={console.error}
        src={files}
        {...props}
      >
        <DropzoneEmptyState />
        <DropzoneContent />
      </Dropzone>
      <div className="mt-2">
        {files?.map((file, i) => (
          <div
            key={i}
            className="flex justify-between text-black bg-muted py-[10px] px-3 rounded-md"
          >
            <div className="flex gap-4 justify-center items-center">
              <div className="p-2 bg-primary/30 rounded-sm">
                <FileIcon size={24} />
              </div>
              <div className="">
                <p className="p-sm">{file.name}</p>
                <p className="p-sm text-primary">{renderBytes(file.size)}</p>
              </div>
            </div>

            <Button type="button" variant="secondary" size="icon">
              <X size={20} />
            </Button>
          </div>
        ))}
      </div>
    </>
  );
}
