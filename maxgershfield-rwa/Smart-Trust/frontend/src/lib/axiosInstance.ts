import axios from "axios";
import Cookies from "js-cookie";
import { API } from "@/lib/constants";

const axiosInstance = axios.create({
  baseURL: API,
  headers: {
    "Content-Type": "application/json",
  },
});

axiosInstance.interceptors.request.use(
  (config) => {
    const token = Cookies.get("oasisToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export default axiosInstance;
