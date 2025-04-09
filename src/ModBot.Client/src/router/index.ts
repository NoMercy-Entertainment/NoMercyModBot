import { createRouter, createWebHistory, type RouteLocationNormalized } from 'vue-router';
import routes from '@/router/routes.ts';
import { user } from '@/store/user.ts';

const router = createRouter({
  history: createWebHistory(import.meta.env.BASE_URL),
  routes: routes
});

router.beforeEach((to: RouteLocationNormalized, from: RouteLocationNormalized) => {
  // Redirect to home if trying to access login while authenticated
  if (to.name === 'Login' && user.value.access_token) {
    return { name: 'home' };
  }

  // Add authentication check for protected routes if needed
  if (to.meta.requiresAuth && !user.value.access_token) {
    return { name: 'Login' };
  }
  
  if (to.name === from.name && to.params.channelName !== from.params.channelName) {
    return { ...to, replace: true };
  }
});

export default router;
