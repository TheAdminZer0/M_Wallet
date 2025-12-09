// In development, always fetch from the network and do not enable offline support.
// This is because caching would make development difficult (changes wouldn't show up).
self.addEventListener('fetch', () => { });
