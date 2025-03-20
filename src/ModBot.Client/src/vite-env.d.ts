import type { ComponentCustomProperties as VueComponentCustomProperties } from '@vue/runtime-core';

declare module '*.svg'
declare module '*.scss';
declare module '*.jpg';
declare module '*.webp';
declare module '*.png';
declare module '*.gif';
declare module '*.json' {
  const value: any;
  export default value;
}

declare module '@vue/runtime-core' {
  interface ComponentCustomProperties extends VueComponentCustomProperties {
    $t: Function<(key?: string, args?: unknown) => string>;
  }
}