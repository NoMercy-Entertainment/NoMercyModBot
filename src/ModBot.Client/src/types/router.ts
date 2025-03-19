import type { RouteRecordRaw } from 'vue-router'

type RouteRecordName<T> = T extends { name: string } ? T['name'] : never;
type RouteRecordPath<T> = T extends { path: string } ? T['path'] : never;

export type InferRouteNames<T extends readonly RouteRecordRaw[]> = {
  [K in keyof T]: RouteRecordName<T[K]>
}[number];

export type InferRoutePaths<T extends readonly RouteRecordRaw[]> = {
  [K in keyof T]: RouteRecordPath<T[K]>
}[number];

// Helper to convert route name to key
export const routeNameToKey = (name: string): string =>
  name.toLowerCase()
    .replace(/[^a-z0-9]/g, '_')
    .replace(/_+/g, '_')
    .replace(/^_|_$/g, '');