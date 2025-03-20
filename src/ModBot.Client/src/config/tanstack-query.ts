import { QueryClient } from '@tanstack/vue-query';

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      refetchOnWindowFocus: false,
      refetchOnMount: false,
      retry: false,
      experimental_prefetchInRender: true,
      staleTime: 1000 * 60 * 60
    }
  }
});
