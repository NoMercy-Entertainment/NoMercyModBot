import { type ComputedRef, type Ref, ref, toRaw, type UnwrapRef } from 'vue';
import { useRoute } from 'vue-router';
import { type QueryKey, useQuery, type UseQueryReturnType } from '@tanstack/vue-query';

import type { AxiosError } from 'axios';
import type { ErrorResponse } from '@/types/server';

import serverClient from './serverClient';

interface InfiniteServerClientProps {
  path?: string;
  data?: any;
  cacheKey?: string;
  keepForever?: boolean;
  limit?: number;
  delay?: number;
  enabled?: boolean;
  queryKey?: string[];
}

export const queryKey = (options?: ServerClientProps | InfiniteServerClientProps) => {
  if (options?.queryKey) return options?.queryKey;

  const route = useRoute();
  const options2: string[] = [];

  (options?.path ?? route.fullPath)?.split('/').slice(1).forEach(p => {
    if (p.includes('?')) {
      options2.push(p.split('?')[0]);
      options2.push(p.split('?')[1].split('=')[1]);
    } else {
      options2.push(p);
    }
  });

  return options?.queryKey ?? options2;
};

export interface ServerClientProps {
  path?: string;
  data?: any;
  cacheKey?: string;
  keepForever?: boolean;
  enabled?: Ref<boolean> | ComputedRef<boolean> | boolean;
  refetchInterval?: number;
  delay?: number;
  queryKey?: QueryKey | unknown[];
  type?: 'get' | 'post' | 'put' | 'patch' | 'delete' | 'head';
  suspense?: Ref<UnwrapRef<boolean>>;
  limit?: number;
}

type Return<T> = UseQueryReturnType<T, AxiosError<ErrorResponse>> extends {
  setLetter?: (value: string) => void;
} ? UseQueryReturnType<T, AxiosError<ErrorResponse>> : UseQueryReturnType<T, AxiosError<ErrorResponse>> & {
  setLetter?: (value: string) => void;
};

const getDataValues = (options?: ServerClientProps) => {
  return Object.keys(options?.data ?? {})
    .reduce((acc, key) => {
      acc[key] = toRaw(options?.data![key].value) ?? options?.data![key];
      return acc;
    }, {} as Record<string | number, any>);
};

const useServerClient = <T, >(options?: ServerClientProps): Return<T> => {

  const route = useRoute();

  const type = ref<'get' | 'post' | 'put' | 'patch' | 'delete' | 'head'>(options?.type ?? 'get');

  const useQueryC = useQuery({
    ...options,
    queryKey: queryKey(options),
    enabled: options?.enabled,
    retry: 0,
    refetchOnMount: true,
    refetchOnWindowFocus: false,
    staleTime: options?.keepForever
      ? Infinity
      : 1000 * 60 * 5,
    queryFn: ({ signal }) => {
      return new Promise<T>((resolve, reject) => {
        setTimeout(() => {
          let promise;

          if ((options?.path ?? route.fullPath).includes('undefined')) return Promise.reject();

          if (type.value == 'get') {
            promise = serverClient<T>()
              .get<T>(
                options?.path ?? route.fullPath,
                {
                  params: {
                    letter: route.query?.letter,
                    ...getDataValues(options)
                  },
                  signal: signal
                }
              )
              .then((response) => {
                // @ts-ignore
                return response.data?.data ?? response.data;
              });

          } else if (type.value == 'post') {

            promise = serverClient<T>()
              .post<T>(options?.path ?? route.fullPath,
                {
                  letter: route.query?.letter ?? undefined,
                  ...getDataValues(options)
                },
                {
                  signal: signal
                })
              .then(({ data }) => {
                // @ts-ignore
                return data?.data ?? data;
              });

          } else if (type.value == 'put') {
            promise = serverClient<T>()
              .put<T>(options?.path ?? route.fullPath,
                {
                  letter: route.query?.letter ?? undefined,
                  ...getDataValues(options)
                },
                {
                  signal: signal
                })
              .then(({ data }) => {
                // @ts-ignore
                return data?.data ?? data;
              });
          } else if (type.value == 'patch') {
            promise = serverClient<T>()
              .patch<T>(options?.path ?? route.fullPath,
                {
                  letter: route.query?.letter ?? undefined,
                  ...getDataValues(options)
                },
                {
                  signal: signal
                })
              .then(({ data }) => {
                // @ts-ignore
                return data?.data ?? data;
              });
          } else if (type.value == 'delete') {
            promise = serverClient<T>()
              .delete<T>(options?.path ?? route.fullPath,
                {
                  letter: route.query?.letter ?? undefined,
                  ...getDataValues(options)
                },
                {
                  signal: signal
                })
              .then(({ data }) => {
                // @ts-ignore
                return data?.data ?? data;
              });
          }

          // promise!.then(async (value) => {
          // 	await set(queryKey(options) as IDBKeyRange | IDBValidKey, value);
          // });

          return resolve(promise as Promise<T>);

        }, options?.delay ?? 0);
      });
    }
  }) as UseQueryReturnType<T, AxiosError<ErrorResponse>>;

  return {
    ...useQueryC
  };

};

export default useServerClient;
