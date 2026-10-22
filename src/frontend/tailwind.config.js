/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        sb: {
          950: '#082433',
          900: '#0b2c42',
          800: '#0d3048',
          700: '#123a55',
          600: '#1b4a68',
          500: '#2b6082'
        },
        acento: {
          DEFAULT: '#d9480f',
          fuerte: '#c2410c',
          claro: '#f76707',
          suave: '#fff4ed'
        },
        lienzo: '#edf0f7',
        tinta: '#12263a'
      },
      fontFamily: {
        sans: ['"Segoe UI"', 'system-ui', '-apple-system', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif']
      },
      boxShadow: {
        panel: '0 18px 45px -28px rgba(11, 44, 66, 0.45)',
        flotante: '0 22px 60px -30px rgba(11, 44, 66, 0.55)'
      },
      borderRadius: {
        panel: '18px'
      },
      keyframes: {
        aparecer: {
          '0%': { opacity: '0', transform: 'translateY(6px)' },
          '100%': { opacity: '1', transform: 'translateY(0)' }
        }
      },
      animation: {
        aparecer: 'aparecer 180ms ease-out'
      }
    }
  },
  plugins: []
};
