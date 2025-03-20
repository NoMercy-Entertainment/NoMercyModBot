import i18next from 'i18next';
import Backend from 'i18next-http-backend';

i18next
  .use(Backend)
  .init({
    lng: navigator.language.split('-')?.[0] ?? 'en',
    fallbackLng: 'en',
    debug: false,
    load: 'currentOnly',
    interpolation: {
      escapeValue: false
    }
  }).then();

export default i18next;