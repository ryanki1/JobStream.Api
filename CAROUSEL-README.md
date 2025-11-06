# 🎪 Infinite Logo Carousel for Carrd.co

A lightweight, responsive, infinite-scrolling logo carousel that can be easily embedded into any Carrd.co landing page.

## ✨ Features

- ✅ **Smooth infinite loop animation** - Seamless scrolling with no visible restart
- ✅ **Pause on hover** - Users can pause to see logos clearly
- ✅ **Fully responsive** - Optimized for mobile, tablet, and desktop
- ✅ **Transparent background** - Integrates seamlessly with any design
- ✅ **Lightweight** - Only ~3KB total, no external dependencies
- ✅ **Dynamic loading** - Add/remove logos by updating a JSON file
- ✅ **GPU-accelerated** - Buttery-smooth 60fps animations
- ✅ **Fallback system** - Works even if manifest file fails to load

## 📦 What's Included

```
├── carousel-demo.html      # Full standalone demo (open in browser to test)
├── carrd-embed.html        # Ready-to-embed code snippet for Carrd
├── manifest.json           # Logo configuration file
└── CAROUSEL-README.md      # This file
```

## 🚀 Quick Start for Carrd.co

### Step 1: Upload Your Logos

Upload your logo images to a web-accessible location:
- Your own server/hosting
- Carrd's asset uploader
- CDN service (Cloudflare, AWS S3, etc.)

### Step 2: Create Your Manifest File

Create a `manifest.json` file with your logo URLs:

```json
{
  "logos": [
    "https://yourdomain.com/logos/logo1.png",
    "https://yourdomain.com/logos/logo2.png",
    "https://yourdomain.com/logos/logo3.png"
  ]
}
```

Upload this file to the same location as your logos.

### Step 3: Embed in Carrd

1. Open your Carrd site in the editor
2. Add an **Embed** element where you want the carousel
3. Set embed type to **Code**
4. Open `carrd-embed.html` and copy the entire contents
5. Paste into the Carrd embed element
6. Update line 97 with your manifest URL:
   ```javascript
   const MANIFEST_URL = 'https://yourdomain.com/manifest.json';
   ```
7. Save and publish!

## 🎨 Customization

### Match Your Site's Background

Edit the fade gradient colors (lines 34-41 in `carrd-embed.html`):

```css
/* For white background */
background: linear-gradient(to right, rgba(255, 255, 255, 1), rgba(255, 255, 255, 0));

/* For dark background */
background: linear-gradient(to right, rgba(0, 0, 0, 1), rgba(0, 0, 0, 0));

/* For custom color (e.g., #667eea) */
background: linear-gradient(to right, rgba(102, 126, 234, 1), rgba(102, 126, 234, 0));
```

### Adjust Logo Size

```css
.logo-carousel-track .logo-item {
  height: 80px;  /* Change this */
}

.logo-carousel-track .logo-item img {
  max-width: 180px;  /* And this */
}
```

### Change Animation Speed

```css
.logo-carousel-track {
  animation: logoScroll 30s linear infinite;  /* 30s = duration for one loop */
}
```

Lower number = faster scroll, higher number = slower scroll.

### Adjust Logo Spacing

```css
.logo-carousel-track {
  gap: 60px;  /* Space between logos */
}
```

### Remove Pause on Hover

Delete lines 55-57:

```css
.logo-carousel-track:hover {
  animation-play-state: paused;
}
```

## 📱 How It Works (Technical)

1. **CSS Animations** - The animation is handled entirely by CSS using `@keyframes`, making it GPU-accelerated and smooth
2. **Duplicate Logos** - Logos are rendered twice in the DOM to create the seamless infinite loop effect
3. **Dynamic Loading** - JavaScript fetches the manifest file and dynamically creates the logo elements
4. **Responsive** - Media queries adjust sizing and speed for mobile devices
5. **Fallback System** - If the manifest fails to load, fallback logos are used so the carousel never breaks

## 🧪 Testing

### Test Locally

1. Open `carousel-demo.html` in your browser
2. You should see the carousel with sample logos
3. Hover over logos to test pause functionality
4. Resize browser window to test responsiveness

### Test on Carrd

1. Follow the Quick Start steps above
2. Use Carrd's preview mode before publishing
3. Test on mobile by using browser dev tools or publishing to a test domain

## 🔧 Troubleshooting

### Logos not loading?

1. **Check manifest URL** - Make sure `MANIFEST_URL` in the embed code points to the correct location
2. **Check CORS** - Your server must allow cross-origin requests. Add this header:
   ```
   Access-Control-Allow-Origin: *
   ```
3. **Check image URLs** - Make sure all URLs in `manifest.json` are publicly accessible
4. **Check browser console** - Open DevTools (F12) and look for error messages

### Animation not smooth?

1. **Too many logos** - Try reducing the number of logos (optimal: 6-12)
2. **Large images** - Optimize your logo files (use compressed PNGs or SVGs)
3. **Slow device** - The animation adjusts automatically, but very old devices may struggle

### Carousel appears broken on mobile?

1. **Check media query** - The responsive breakpoint is at 768px (line 82)
2. **Test gap spacing** - Mobile uses smaller gaps (40px vs 60px)
3. **Check logo sizes** - Mobile limits logos to 120px width

### Background fade doesn't match my site?

Update the gradient colors in lines 34-35 and 40-41 to match your site's exact background color.

## 📊 Browser Support

- ✅ Chrome/Edge 90+
- ✅ Firefox 88+
- ✅ Safari 14+
- ✅ iOS Safari 14+
- ✅ Chrome Mobile 90+

## 💡 Tips for Best Results

1. **Logo formats** - Use PNG (with transparency) or SVG for best quality
2. **File size** - Keep each logo under 50KB for fast loading
3. **Consistent sizing** - Make all logos roughly the same height for visual consistency
4. **Quantity** - 6-12 logos works best (too few looks empty, too many looks cluttered)
5. **Contrast** - Ensure logos are visible against your background (you can add filters in CSS)

## 🎯 Adding/Removing Logos

Simply edit your `manifest.json` file:

```json
{
  "logos": [
    "https://yourdomain.com/logo1.png",
    "https://yourdomain.com/logo2.png",
    "https://yourdomain.com/logo3.png"
    // Add more here...
  ]
}
```

The carousel will automatically pick up changes on the next page load. No code changes needed!

## 🛠️ Advanced: Alternative to Manifest File

If you can't host a manifest file, you can hardcode the logos directly in the embed code.

Replace lines 96-116 in `carrd-embed.html` with:

```javascript
async function initLogoCarousel() {
  const track = document.getElementById('logoCarouselTrack');
  if (!track) return;

  // Hardcoded logos (no manifest needed)
  const logos = [
    'https://yourdomain.com/logo1.png',
    'https://yourdomain.com/logo2.png',
    'https://yourdomain.com/logo3.png',
    'https://yourdomain.com/logo4.png',
    'https://yourdomain.com/logo5.png',
    'https://yourdomain.com/logo6.png'
  ];

  // ... rest of the function stays the same
```

**Note:** This means you'll need to update the embed code in Carrd each time you add/remove logos.

## 📞 Support

If you encounter any issues:

1. Check the troubleshooting section above
2. Open your browser's DevTools console (F12) and look for errors
3. Verify all URLs are correct and publicly accessible
4. Test with the standalone `carousel-demo.html` first

## 📄 License

This code is provided as-is for use in your Carrd.co project. Feel free to modify and customize as needed.

---

**Built with ❤️ using vanilla JavaScript, CSS3 animations, and modern ES6+ features.**

**No jQuery. No React. No bloat. Just clean, performant code.**
