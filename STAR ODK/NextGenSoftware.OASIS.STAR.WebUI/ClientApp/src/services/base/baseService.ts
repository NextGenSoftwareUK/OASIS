/**
 * Base Service Class
 * Shared functionality for all service classes
 */

import { AxiosResponse } from 'axios';
import { starApiClient, web4ApiClient, hubApiClient } from './apiClient';
import { isDemoMode } from '../config/demoConfig';
import { OASISResult } from '../../types/star';

export abstract class BaseService {
  protected starApi = starApiClient;
  protected web4Api = web4ApiClient;
  protected hubApi = hubApiClient;

  /**
   * Handle API requests with demo mode support
   */
  protected async handleRequest<T>(
    apiCall: () => Promise<AxiosResponse<OASISResult<T>>>,
    demoData: T,
    demoMessage?: string
  ): Promise<OASISResult<T>> {
    if (isDemoMode()) {
      // Simulate network delay in demo mode
      await new Promise(resolve => setTimeout(resolve, 100));
      
      return {
        result: demoData,
        isError: false,
        message: demoMessage || 'Demo mode response',
      };
    }

    try {
      const response = await apiCall();
      return response.data;
    } catch (error: any) {
      console.error('API request failed:', error);
      return {
        result: undefined,
        isError: true,
        message: error.response?.data?.message || error.message || 'Request failed',
        exception: error,
      };
    }
  }

  /**
   * Handle API requests that return arrays
   */
  protected async handleArrayRequest<T>(
    apiCall: () => Promise<AxiosResponse<OASISResult<T[]>>>,
    demoData: T[],
    demoMessage?: string
  ): Promise<OASISResult<T[]>> {
    return this.handleRequest(apiCall, demoData, demoMessage);
  }

  /**
   * Handle API requests that return boolean
   */
  protected async handleBooleanRequest(
    apiCall: () => Promise<AxiosResponse<OASISResult<boolean>>>,
    demoData: boolean = true,
    demoMessage?: string
  ): Promise<OASISResult<boolean>> {
    return this.handleRequest(apiCall, demoData, demoMessage);
  }

  /**
   * Create request payload with common fields
   */
  protected createPayload(data: any, additionalFields: any = {}): any {
    return {
      ...data,
      ...additionalFields,
      timestamp: new Date().toISOString(),
    };
  }

  /**
   * Extract data from OASISResult
   */
  protected extractResult<T>(result: OASISResult<T>): T | undefined {
    if (result.isError) {
      throw new Error(result.message);
    }
    return result.result;
  }

  /**
   * Check if result is successful
   */
  protected isSuccess<T>(result: OASISResult<T>): boolean {
    return !result.isError && result.result !== undefined;
  }

  /**
   * HTTP GET request
   */
  protected async httpGet<T>(url: string): Promise<T> {
    const response = await this.starApi.get<OASISResult<T>>(url);
    if (response.data.isError) {
      throw new Error(response.data.message);
    }
    return response.data.result!;
  }

  /**
   * HTTP POST request
   */
  protected async httpPost<T>(url: string, data?: any): Promise<T> {
    const response = await this.starApi.post<OASISResult<T>>(url, data);
    if (response.data.isError) {
      throw new Error(response.data.message);
    }
    return response.data.result!;
  }

  /**
   * HTTP PUT request
   */
  protected async httpPut<T>(url: string, data?: any): Promise<T> {
    const response = await this.starApi.put<OASISResult<T>>(url, data);
    if (response.data.isError) {
      throw new Error(response.data.message);
    }
    return response.data.result!;
  }

  /**
   * HTTP DELETE request
   */
  protected async httpDelete<T>(url: string): Promise<T> {
    const response = await this.starApi.delete<OASISResult<T>>(url);
    if (response.data.isError) {
      throw new Error(response.data.message);
    }
    return response.data.result!;
  }
}
