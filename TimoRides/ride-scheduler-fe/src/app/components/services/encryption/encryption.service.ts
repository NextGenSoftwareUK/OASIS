import { Injectable } from '@angular/core';
import * as CryptoJS from 'crypto-js';

@Injectable({
  providedIn: 'root',
})
export class EncryptionService {
  private secretKey = 'your-secret-key'; // Use a strong, unique key for encryption

  constructor() {}

  setItem(key: string, data: any): void {
    const encryptedData = this.encryptData(data);
    sessionStorage.setItem(key, encryptedData);
  }

  getItem(key: string): any {
    const encryptedData = sessionStorage.getItem(key);
    if (encryptedData) {
      return this.decryptData(encryptedData);
    }
    return null;
  }

  removeItem(key: string): void {
    sessionStorage.removeItem(key);
  }

  clear(): void {
    sessionStorage.clear();
  }

  encryptData(data: any): string {
    const stringifiedData = JSON.stringify(data);
    return CryptoJS.AES.encrypt(stringifiedData, this.secretKey).toString();
  }

  decryptData(encryptedData: string): any {
    const bytes = CryptoJS.AES.decrypt(encryptedData, this.secretKey);
    const decryptedData = bytes.toString(CryptoJS.enc.Utf8);
    return JSON.parse(decryptedData);
  }
}
