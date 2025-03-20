import type { RouteRecordRaw } from 'vue-router';

import {
  HomeIcon,
  FolderIcon,
  Cog6ToothIcon,
  UserCircleIcon,
  UsersIcon,
  ArrowLeftStartOnRectangleIcon
} from '@heroicons/vue/24/outline';

import Home from '@/views/Home.vue';
import Login from '@/views/Auth/Login.vue';
import Unauthenticated from '@/views/Auth/Unauthenticated.vue';
import { type InferRouteNames, type InferRoutePaths, routeNameToKey } from '@/types/router';
import NotFound from '@/views/NotFound.vue';

const mainRoutes: Array<RouteRecordRaw> = [
  {
    path: '/',
    redirect: {
      name: 'Unauthenticated'
    }
  },
  {
    path: '/home',
    name: 'Home',
    component: Home,
    meta: {
      requiresAuth: true,
      group: 'main',
      icon: HomeIcon
    }
  },
  {
    path: '/moderation/channels',
    name: 'Channels',
    component: () => import('@/views/Channels.vue'),
    meta: {
      requiresAuth: true,
      group: 'main',
      icon: UsersIcon
    }
  },
  {
    path: '/moderation/channels/:channelLogin',
    name: 'Channel',
    component: () => import('@/views/Channel.vue'),
    meta: {
      requiresAuth: true,
      icon: UsersIcon
    }
  },
  {
    path: '/about',
    name: 'About',
    component: () => import('@/views/About.vue'),
    meta: {
      requiresAuth: false,
      group: 'main',
      icon: FolderIcon
    }
  },
  {
    path: '/settings',
    name: 'Settings',
    component: () => import('@/views/Settings.vue'),
    meta: {
      requiresAuth: true,
      group: 'main',
      icon: Cog6ToothIcon,
      menuItem: true
    }
  },
  {
    path: '/:catchAll(.*)*',
    component: NotFound,
    props: {
      message: 'Page not found',
      status: 404
    }
  }
];

const authRoutes: Array<RouteRecordRaw> = [
  {
    path: '/auth',
    name: 'Unauthenticated',
    component: Unauthenticated,
    meta: {
      group: 'auth'
    }
  },
  {
    path: '/auth/login',
    name: 'Login',
    component: Login,
    meta: {
      group: 'auth'
    }
  },
  {
    path: '/auth/callback',
    redirect: to => {
      return { path: '/auth/login', query: { ...to.query } };
    },
    meta: {
      group: 'auth'
    }
  },
  {
    path: '/auth/logout',
    name: 'Sign out',
    component: () => import('@/views/Auth/Logout.vue'),
    meta: {
      requiresAuth: true,
      group: 'profileMenu',
      icon: ArrowLeftStartOnRectangleIcon,
      menuItem: true
    }
  }
];
const userRoutes: Array<RouteRecordRaw> = [];

const legalRoutes: Array<RouteRecordRaw> = [
  {
    path: '/terms',
    name: 'Terms of Service',
    component: () => import('@/views/Legal/Terms.vue'),
    meta: {
      icon: UserCircleIcon
    }
  },
  {
    path: '/privacy',
    name: 'Privacy Policy',
    component: () => import('@/views/Legal/Privacy.vue'),
    meta: {
      icon: UserCircleIcon
    }
  }
];

// Combine all routes
export const routes: Array<RouteRecordRaw> = [
  ...userRoutes,
  ...authRoutes,
  ...legalRoutes,
  ...mainRoutes
];

export default routes;

export type RouteName = InferRouteNames<typeof routes>;
export type RoutePath = InferRoutePaths<typeof routes>;

export const getRoutePaths = (routes: RouteRecordRaw[]) => {
  const paths = routes.reduce((acc, route) => {
    if (route.name && route.path) {
      const key = routeNameToKey(route.name as string);
      acc[key] = route.path;
    }
    return acc;
  }, {} as Record<string, string>);

  return paths as { [K in typeof routes[number]['name'] as string]: (typeof routes[number])['path'] };
};

export const getRouteNames = (routes: RouteRecordRaw[]): Record<string, RouteName> => {
  return routes.reduce((acc, route) => {
    if (route.name) {
      acc[route.name.toString().toLowerCase().replace(/\s+/g, '_')] = route.name as RouteName;
    }
    return acc;
  }, {} as Record<string, RouteName>);
};

export const routeNames = getRouteNames(routes);
export const routePaths = getRoutePaths(routes);
