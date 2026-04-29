/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './**/*.razor',
    './**/*.html',
    './**/*.cs',
  ],
  safelist: [
    'badge-success', 'badge-warning', 'badge-error', 'badge-ghost', 'badge-info',
    'btn-primary', 'btn-outline', 'btn-ghost', 'btn-error', 'btn-success',
    'btn-warning', 'btn-neutral', 'btn-sm', 'btn-xs', 'btn-square',
    'bg-success', 'bg-warning', 'bg-error',
    'border-l-4', 'border-l-primary', 'border-l-success', 'border-l-error',
    'border-l-warning', 'border-l-base-300',
    'border-2', 'bg-primary/10', 'text-primary',
    'alert-success', 'alert-error', 'alert-warning', 'alert-info',
    'text-error', 'text-success', 'text-warning',
    'tab-active',
  ],
  theme: {
    extend: {
      fontFamily: {
        sans: ['Segoe UI', 'system-ui', 'sans-serif'],
      },
      width: {
        sidebar: '220px',
      },
      height: {
        topbar: '52px',
      },
    },
  },
  plugins: [
    require('daisyui'),
  ],
  daisyui: {
    themes: ['lemonade', 'business'],
    darkTheme: 'business',
    base: true,
    styled: true,
    utils: true,
    logs: false,
  },
}
