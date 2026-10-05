import axios from 'axios';

const SMART_CONTRACT_API_URL = 'http://localhost:5257/api/v1/contracts';

export interface TrustContractData {
  trustData: any;
  propertyData: any;
  tokenData: any;
  beneficiaryData: any;
}

export class TrustService {
  static async generateContract(data: TrustContractData): Promise<string> {
    try {
      const response = await axios.post(`${SMART_CONTRACT_API_URL}/generate`, {
        contractType: 'WyomingTrustTokenization',
        blockchain: 'Kadena',
        ...data
      });
      return response.data;
    } catch (error) {
      console.error('Error generating contract:', error);
      throw new Error('Failed to generate contract');
    }
  }

  static async compileContract(contractCode: string): Promise<any> {
    try {
      const formData = new FormData();
      formData.append('sourceCode', contractCode);
      formData.append('language', 'Solidity');
      
      const response = await axios.post(`${SMART_CONTRACT_API_URL}/compile`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return response.data;
    } catch (error) {
      console.error('Error compiling contract:', error);
      throw new Error('Failed to compile contract');
    }
  }

  static async deployContract(abi: any, bytecode: string): Promise<string> {
    try {
      const formData = new FormData();
      formData.append('abi', JSON.stringify(abi));
      formData.append('bytecode', bytecode);
      formData.append('language', 'Solidity');
      
      const response = await axios.post(`${SMART_CONTRACT_API_URL}/deploy`, formData, {
        headers: { 'Content-Type': 'multipart/form-data' }
      });
      return response.data.contractAddress;
    } catch (error) {
      console.error('Error deploying contract:', error);
      throw new Error('Failed to deploy contract');
    }
  }
}
