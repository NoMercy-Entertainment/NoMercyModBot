import type { HttpStatusCode } from 'axios';

export interface ErrorResponse {
  type: string;
  title: string;
  status: HttpStatusCode,
  detail: string;
  instance: string;
  traceId: string;
}