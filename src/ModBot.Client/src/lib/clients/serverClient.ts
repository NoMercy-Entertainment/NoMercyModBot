import client from '@/lib/clients/client';

export default <T>(timeout?: number) => {
  return client<T>('/api', timeout);
};

