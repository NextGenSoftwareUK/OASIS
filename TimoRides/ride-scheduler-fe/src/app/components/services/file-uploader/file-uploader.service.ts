import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { fileUploadURL } from 'src/app/base-url/base-url';
import {
  FileUploadRequest,
  FileUploaderResponse,
} from 'src/app/models/file-uploader';

@Injectable({
  providedIn: 'root',
})
export class FileUploaderService {
  constructor(private http: HttpClient) {}

  uploadImage(body: FileUploadRequest): Observable<FileUploaderResponse> {
    return this.http.post<FileUploaderResponse>(fileUploadURL, body);
  }
}
