import axios, { type AxiosRequestConfig } from "axios";
import { useIdentityStore } from "@/stores/identity.store";

export const axiosInstance = axios.create({
    baseURL: "",
    headers: {
        "Content-Type": "application/json",
    },
});

axiosInstance.interceptors.request.use(
    (config) => {
        const accessToken = useIdentityStore.getState().accessToken;

        if (accessToken && config.headers) {
            config.headers.Authorization = `Bearer ${accessToken}`;
        }

        return config;
    },
    (error) => Promise.reject(error),
);

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
