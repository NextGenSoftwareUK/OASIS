import axios from "axios";
import Cookies from "js-cookie";
import { API } from "@/lib/constants";

const axiosInstanceForFiles = axios.create({
  baseURL: "/api", // Use local Next.js API routes
  headers: {
    "Content-Type": "multipart/form-data",
  },
});

axiosInstanceForFiles.interceptors.request.use(
  (config) => {
    const token = Cookies.get("oasisToken");
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

export default axiosInstanceForFiles;
