import axios, { type AxiosRequestConfig } from "axios";
import { getCsrfToken, login } from "./auth-session";

export const axiosInstance = axios.create({
    baseURL: "",
    withCredentials: true,
    headers: {
        "Content-Type": "application/json",
    },
});

axiosInstance.interceptors.request.use(
    (config) => {
        const method = config.method?.toUpperCase() ?? "GET";
        if (!["GET", "HEAD", "OPTIONS"].includes(method)) {
            const token = getCsrfToken();
            if (token) config.headers.set("X-CSRF-TOKEN", token);
        }

        return config;
    },
    (error) => Promise.reject(error),
);

axiosInstance.interceptors.response.use(response => response, error => {
    if (axios.isAxiosError(error) && error.response?.status === 401) login();
    return Promise.reject(error);
});

export const apiClient = axiosInstance;

export const customInstance = async <T>(
    config: AxiosRequestConfig,
): Promise<T> => {
    try {
        const { data } = await apiClient(config);
        return data;
    } catch (error) {
        if (axios.isAxiosError(error)) {
            throw error.response?.data;
        }
        throw error;
    }
};

export default customInstance;
