/**
 * Converts an image file to a Base64 string.
 * @param file - The image file to convert.
 * @returns A promise that resolves to the Base64 string representation of the image.
 */
export const imageToBase64 = (file: File): Promise<string> => {
  return new Promise((resolve, reject) => {
    const reader = new FileReader();
    reader.onload = () => {
      if (reader.result) {
        resolve(reader.result.toString());
      } else {
        reject(new Error('Failed to convert image to Base64'));
      }
    };
    reader.onerror = (error) => reject(error);
    reader.readAsDataURL(file);
  });
};
