import axios, { type AxiosRequestConfig, type AxiosResponse } from 'axios';
import { storeTwitchUser, user } from '@/store/user';
import authService from '@/services/authService';

export interface AxiosInstance<E> {
  request<T = unknown>(config: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  get<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  delete<T = unknown, S = unknown>(url: string, data?: S, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  head<T = unknown>(url: string, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  post<T = unknown, S = unknown>(url: string, data?: S, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  put<T = unknown, S = unknown>(url: string, data: S, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;

  patch<T = unknown, S = unknown>(url: string, data: S, config?: AxiosRequestConfig): Promise<AxiosResponse<T, E>>;
}

export default <T>(baseUrl: string, timeout?: number) => {
  const language = localStorage.getItem('displayLanguage')?.replace(/"/gu, '') || navigator.language.split('-')?.[0];

  const axiosInstance = axios.create({
    headers: {
      Accept: 'application/json',
      'Accept-Language': language,
      Authorization: `Bearer ${user.value?.access_token}`
    },
    timeout: timeout,
    baseURL: baseUrl
  });

  axiosInstance.interceptors.request.use(config => {
    if (user.value.access_token) {
      config.headers.Authorization = `Bearer ${user.value.access_token}`;
    }
    return config;
  }, async (error) => {
    if (error.response?.status === 401) {
      await authService.refreshToken()
        .then((data) => {
          storeTwitchUser(data.user);
          return axios.request(error.config);
        })
        .catch(() => {
          return Promise.reject(error);
        });
    } else {
      return Promise.reject(error);
    }
  });

  return axiosInstance as AxiosInstance<T>;
};


