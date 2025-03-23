
export const setThemeColor = (color: string) => {
  document.documentElement.style.setProperty('--theme', color);
}