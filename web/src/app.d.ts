// See https://svelte.dev/docs/kit/types#app.d.ts
declare global {
  namespace App {
    interface Locals {
      token?: string;
      username?: string;
    }
    // interface Error {}
    // interface PageData {}
    // interface Platform {}
  }
}

export {};
