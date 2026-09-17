/**
 * ════════════════════════════════════════════════════════════════════════════════
 * WORK TRACKER PRO — DYNAMIC THEME & FONT MANAGER (40 THEMES & 5 GOOGLE FONTS)
 * ════════════════════════════════════════════════════════════════════════════════
 */

const FONTS_CONFIG = [
    {
        id: "inter",
        name: "Inter",
        badge: "Default UI",
        desc: "Standar UI modern, sangat seimbang, tajam & nyaman dibaca pada semua resolusi.",
        fontFamily: "'Inter', system-ui, sans-serif",
        sample: "Work Tracker Pro 2026 - Efisiensi & Produktivitas Tim"
    },
    {
        id: "jakarta",
        name: "Plus Jakarta Sans",
        badge: "Modern Enterprise",
        desc: "Font geometris kontemporer, ramah & elegan khas aplikasi SaaS modern.",
        fontFamily: "'Plus Jakarta Sans', sans-serif",
        sample: "Work Tracker Pro 2026 - Efisiensi & Produktivitas Tim"
    },
    {
        id: "outfit",
        name: "Outfit",
        badge: "Contemporary",
        desc: "Tipografi sans-serif modern dengan lekukan halus dan visual berkelas tinggi.",
        fontFamily: "'Outfit', sans-serif",
        sample: "Work Tracker Pro 2026 - Efisiensi & Produktivitas Tim"
    },
    {
        id: "poppins",
        name: "Poppins",
        badge: "Geometric Rounded",
        desc: "Bentuk geometris rounded yang bersahabat, energik, dan mudah dipindai mata.",
        fontFamily: "'Poppins', sans-serif",
        sample: "Work Tracker Pro 2026 - Efisiensi & Produktivitas Tim"
    },
    {
        id: "roboto",
        name: "Roboto",
        badge: "High Density",
        desc: "Klasik Google yang presisi, efisien dengan densitas informasi tinggi.",
        fontFamily: "'Roboto', sans-serif",
        sample: "Work Tracker Pro 2026 - Efisiensi & Produktivitas Tim"
    }
];

const THEMES_CONFIG = [
    // ── DARK THEMES (18 THEMES: 6 ORIGINAL + 12 NEW EYE-FRIENDLY) ───────────────
    {
        id: "dark-nordic",
        name: "Nordic Frost",
        desc: "Slate dingin pekat & aksen polar blue lembut anti-glare",
        icon: "fa-snowflake",
        colors: ["#38BDF8", "#64748B", "#1E293B"],
        tag: "Eye-Friendly Dark",
        category: "dark"
    },
    {
        id: "dark-graphite",
        name: "Graphite Minimal",
        desc: "Abu-abu arang netral & tipografi perak tidak menyilaukan",
        icon: "fa-layer-group",
        colors: ["#A1A1AA", "#52525B", "#18181B"],
        tag: "Neutral Charcoal",
        category: "dark"
    },
    {
        id: "dark-titanium",
        name: "Titanium Industrial",
        desc: "Abu-abu aerospace & aksen bronze hangat yang teduh",
        icon: "fa-shield-halved",
        colors: ["#D97706", "#78716C", "#1C1917"],
        tag: "Warm Dark",
        category: "dark"
    },
    {
        id: "dark-mocha",
        name: "Mocha Espresso",
        desc: "Cokelat arang gelap & nuansa latte menenangkan",
        icon: "fa-mug-hot",
        colors: ["#A8A29E", "#B45309", "#1C1412"],
        tag: "Warm Dark",
        category: "dark"
    },
    {
        id: "dark-sage",
        name: "Dark Sage Oasis",
        desc: "Olive slate gelap & hijau eucalyptus lembut ramah mata",
        icon: "fa-seedling",
        colors: ["#34D399", "#64748B", "#131C18"],
        tag: "Nature Dark",
        category: "dark"
    },
    {
        id: "dark-slate-pro",
        name: "Slate Professional",
        desc: "Steel navy gelap & aksen corporate cobalt muted",
        icon: "fa-briefcase",
        colors: ["#60A5FA", "#64748B", "#0F172A"],
        tag: "Corporate Dark",
        category: "dark"
    },
    {
        id: "dark-lavender",
        name: "Midnight Lavender",
        desc: "Charcoal wisteria gelap & aksen lilac lembut",
        icon: "fa-moon",
        colors: ["#C084FC", "#64748B", "#171324"],
        tag: "Pastel Dark",
        category: "dark"
    },
    {
        id: "dark-moss",
        name: "Deep Forest Moss",
        desc: "Hijau lumut hutan pinus gelap & aksen sage sejuk",
        icon: "fa-tree",
        colors: ["#10B981", "#4B5563", "#0B1A13"],
        tag: "Nature Dark",
        category: "dark"
    },
    {
        id: "dark-sandstone",
        name: "Desert Sandstone Dark",
        desc: "Abu-abu gurun hangat & aksen terracotta redup",
        icon: "fa-mountain-sun",
        colors: ["#F59E0B", "#78716C", "#1A1715"],
        tag: "Warm Dark",
        category: "dark"
    },
    {
        id: "dark-monochrome",
        name: "Monochrome Studio",
        desc: "True neutral grayscale murni & kontras anti-fatigue",
        icon: "fa-circle-half-stroke",
        colors: ["#E4E4E7", "#71717A", "#121212"],
        tag: "Grayscale",
        category: "dark"
    },
    {
        id: "dark-aurora",
        name: "Subtle Aurora",
        desc: "Deep teal charcoal & aksen aurora mint pastel",
        icon: "fa-water",
        colors: ["#2DD4BF", "#64748B", "#0C181C"],
        tag: "Teal Dark",
        category: "dark"
    },
    {
        id: "dark-velvet",
        name: "Velvet Noir",
        desc: "Charcoal mawar redup & aksen dusty rose mauve",
        icon: "fa-gem",
        colors: ["#FB7185", "#64748B", "#1C1217"],
        tag: "Pastel Dark",
        category: "dark"
    },
    {
        id: "dark-midnight",
        name: "Midnight OLED",
        desc: "Hitam karbon OLED pekat & aksen indigo",
        icon: "fa-moon",
        colors: ["#6366F1", "#38BDF8", "#0B0F17"],
        tag: "Dark OLED",
        category: "dark"
    },
    {
        id: "dark-cyberpunk",
        name: "Cyberpunk Synthwave",
        desc: "Obsidian neon fuchsia & electric cyan",
        icon: "fa-bolt",
        colors: ["#F43F5E", "#06B6D4", "#100C1F"],
        tag: "Dark Neon",
        category: "dark"
    },
    {
        id: "dark-matrix",
        name: "Emerald Matrix",
        desc: "Hacker matrix gelap & emerald menyala",
        icon: "fa-terminal",
        colors: ["#10B981", "#34D399", "#051A14"],
        tag: "Dark Green",
        category: "dark"
    },
    {
        id: "dark-dracula",
        name: "Dracula Eclipse",
        desc: "Ungu malam gelap dengan kontras pastel",
        icon: "fa-ghost",
        colors: ["#A855F7", "#EC4899", "#150E24"],
        tag: "Dark Purple",
        category: "dark"
    },
    {
        id: "dark-abyss",
        name: "Abyssal Ocean",
        desc: "Samudra dalam & sapphire futuristik",
        icon: "fa-water",
        colors: ["#38BDF8", "#3B82F6", "#071224"],
        tag: "Dark Blue",
        category: "dark"
    },
    {
        id: "dark-ember",
        name: "Solar Ember",
        desc: "Lava arang gelap & api oranye emas",
        icon: "fa-fire",
        colors: ["#F97316", "#F59E0B", "#170E08"],
        tag: "Dark Warm",
        category: "dark"
    },

    // ── LIGHT THEMES (22 THEMES: 10 ORIGINAL + 12 NEW EYE-FRIENDLY) ──────────────
    {
        id: "light-alabaster",
        name: "Alabaster Warm",
        desc: "Putih perkamen hangat & stone gray tidak menyilaukan",
        icon: "fa-book-open",
        colors: ["#57534E", "#A8A29E", "#FBFBF9"],
        tag: "Warm Neutral",
        category: "light"
    },
    {
        id: "light-sage",
        name: "Soothing Sage",
        desc: "Muted sage green & aksen daun laurel eucalyptus teduh",
        icon: "fa-leaf",
        colors: ["#059669", "#64748B", "#F2F7F4"],
        tag: "Soothing Green",
        category: "light"
    },
    {
        id: "light-slate",
        name: "Neutral Slate Clean",
        desc: "Cool slate gray lembut & aksen baja kebiruan rapi",
        icon: "fa-layer-group",
        colors: ["#475569", "#3B82F6", "#F1F5F9"],
        tag: "Clean Slate",
        category: "light"
    },
    {
        id: "light-oatmeal",
        name: "Oatmeal Minimal",
        desc: "Beige oat alami & aksen neutral flax linen tenang",
        icon: "fa-wheat-awn",
        colors: ["#78716C", "#A8A29E", "#F8F6F0"],
        tag: "Natural Beige",
        category: "light"
    },
    {
        id: "light-mist",
        name: "Morning Mist",
        desc: "Abu-abu embun pagi & aksen powder blue sejuk",
        icon: "fa-cloud",
        colors: ["#64748B", "#0284C7", "#F0F4F8"],
        tag: "Airy Blue",
        category: "light"
    },
    {
        id: "light-porcelain",
        name: "Porcelain Ceramic",
        desc: "Abu-abu keramik bersih & garis grafit minimalis",
        icon: "fa-circle-notch",
        colors: ["#3F3F46", "#71717A", "#FAFAFA"],
        tag: "Minimal Ceramic",
        category: "light"
    },
    {
        id: "light-dune",
        name: "Desert Dune",
        desc: "Pasir gurun hangat & aksen bronze terracotta",
        icon: "fa-sun",
        colors: ["#D97706", "#78716C", "#FAF7F2"],
        tag: "Warm Sand",
        category: "light"
    },
    {
        id: "light-pewter",
        name: "Soft Pewter",
        desc: "Perak abu-abu arsitektural & aksen gunmetal netral",
        icon: "fa-cube",
        colors: ["#4B5563", "#9CA3AF", "#EFF1F3"],
        tag: "Silver Gray",
        category: "light"
    },
    {
        id: "light-lavender-mist",
        name: "Lavender Mist",
        desc: "Wisteria pastel lembut & aksen lilac kalem",
        icon: "fa-wand-magic-sparkles",
        colors: ["#7C3AED", "#64748B", "#F7F5FB"],
        tag: "Pastel Lilac",
        category: "light"
    },
    {
        id: "light-arctic",
        name: "Arctic Breeze",
        desc: "Glacier putih es lembut & aksen seafoam sejuk",
        icon: "fa-compass",
        colors: ["#0D9488", "#475569", "#EDF6F9"],
        tag: "Fresh Teal",
        category: "light"
    },
    {
        id: "light-paper",
        name: "Vintage Paper",
        desc: "Ivory cream buku klasik & teks sepia anti-silau",
        icon: "fa-feather",
        colors: ["#44403C", "#A16207", "#FDFBF7"],
        tag: "Vintage Book",
        category: "light"
    },
    {
        id: "light-denim",
        name: "Calm Denim",
        desc: "Biru denim stonewash lembut & aksen navy terukur",
        icon: "fa-shirt",
        colors: ["#1E3A8A", "#64748B", "#F0F3F9"],
        tag: "Denim Blue",
        category: "light"
    },
    {
        id: "indigo",
        name: "Indigo Nebula",
        desc: "Indigo modern, elegan & fokus",
        icon: "fa-sparkles",
        colors: ["#6366F1", "#8B5CF6", "#FFFFFF"],
        tag: "Light",
        category: "light"
    },
    {
        id: "emerald",
        name: "Emerald Forest",
        desc: "Hijau alam segar & tenang",
        icon: "fa-leaf",
        colors: ["#10B981", "#0D9488", "#FFFFFF"],
        tag: "Nature",
        category: "light"
    },
    {
        id: "ocean",
        name: "Ocean Azure",
        desc: "Biru laut cerah & profesional",
        icon: "fa-water",
        colors: ["#0284C7", "#2563EB", "#FFFFFF"],
        tag: "Corporate",
        category: "light"
    },
    {
        id: "sunset",
        name: "Sunset Crimson",
        desc: "Merah hangat, berani & energik",
        icon: "fa-sun",
        colors: ["#F43F5E", "#EA580C", "#FFFFFF"],
        tag: "Warm",
        category: "light"
    },
    {
        id: "cyberpunk",
        name: "Cyberpunk Neon",
        desc: "Ungu fuchsia futuristik & cerah",
        icon: "fa-bolt",
        colors: ["#D946EF", "#06B6D4", "#FFFFFF"],
        tag: "Vibrant",
        category: "light"
    },
    {
        id: "amethyst",
        name: "Royal Amethyst",
        desc: "Ungu royal eksklusif & mewah",
        icon: "fa-gem",
        colors: ["#9333EA", "#6366F1", "#FFFFFF"],
        tag: "Luxury",
        category: "light"
    },
    {
        id: "amber",
        name: "Amber Gold",
        desc: "Kuning emas hangat & optimis",
        icon: "fa-crown",
        colors: ["#F59E0B", "#EA580C", "#FFFFFF"],
        tag: "Golden",
        category: "light"
    },
    {
        id: "slate",
        name: "Slate Minimalist",
        desc: "Abu-abu slate monokrom & bersih",
        icon: "fa-sliders",
        colors: ["#475569", "#2563EB", "#FFFFFF"],
        tag: "Minimalist",
        category: "light"
    },
    {
        id: "teal",
        name: "Nordic Teal",
        desc: "Cyan toska sejuk & menenangkan",
        icon: "fa-wind",
        colors: ["#0D9488", "#0284C7", "#FFFFFF"],
        tag: "Calm",
        category: "light"
    },
    {
        id: "rose",
        name: "Rose Quartz",
        desc: "Pink lembut, anggun & bersahabat",
        icon: "fa-heart",
        colors: ["#E11D48", "#9333EA", "#FFFFFF"],
        tag: "Elegant",
        category: "light"
    }
];

const ThemeManager = {
    currentTheme: "indigo",
    currentFont: "inter",
    dropdownOpen: false,

    init: function () {
        this.currentTheme = localStorage.getItem('app_theme') || 'indigo';
        this.currentFont = localStorage.getItem('app_font') || 'inter';

        this.applyTheme(this.currentTheme);
        this.applyFont(this.currentFont);
        this.renderDropdown();
        this.updateTopbarSwatch();

        // Close dropdown when clicked outside
        document.addEventListener('click', (e) => {
            const dropdown = document.getElementById('themeDropdownMenu');
            const btn = document.getElementById('themeDropdownBtn');
            if (dropdown && btn && !dropdown.contains(e.target) && !btn.contains(e.target)) {
                this.closeDropdown();
            }
        });
    },

    // ── THEME MANAGEMENT ──────────────────────────────────────────
    applyTheme: function (themeId) {
        const theme = THEMES_CONFIG.find(t => t.id === themeId) || THEMES_CONFIG[0];
        this.currentTheme = theme.id;
        document.documentElement.setAttribute('data-theme', theme.id);
        localStorage.setItem('app_theme', theme.id);
        this.updateTopbarSwatch();
        this.renderDropdown();
    },

    updateTopbarSwatch: function () {
        const swatch = document.getElementById('topbar-theme-swatch');
        const label = document.getElementById('topbar-theme-label');
        const theme = THEMES_CONFIG.find(t => t.id === this.currentTheme);

        if (swatch && theme) {
            swatch.style.background = `linear-gradient(135deg, ${theme.colors[0]}, ${theme.colors[1]})`;
        }
        if (label && theme) {
            label.textContent = theme.name;
        }
    },

    toggleDropdown: function () {
        this.dropdownOpen = !this.dropdownOpen;
        const menu = document.getElementById('themeDropdownMenu');
        if (menu) {
            if (this.dropdownOpen) {
                menu.classList.remove('hidden');
                menu.classList.add('theme-modal-enter-active');
            } else {
                menu.classList.add('hidden');
                menu.classList.remove('theme-modal-enter-active');
            }
        }
    },

    closeDropdown: function () {
        this.dropdownOpen = false;
        const menu = document.getElementById('themeDropdownMenu');
        if (menu) {
            menu.classList.add('hidden');
            menu.classList.remove('theme-modal-enter-active');
        }
    },

    renderDropdown: function () {
        const grid = document.getElementById('themeDropdownGrid');
        if (!grid) return;

        grid.innerHTML = '';

        // Group into Dark and Light
        const darkThemes = THEMES_CONFIG.filter(t => t.category === 'dark');
        const lightThemes = THEMES_CONFIG.filter(t => t.category === 'light');

        // Dark Section Header
        const darkHeader = document.createElement('div');
        darkHeader.className = 'px-3 py-1.5 text-[10px] font-black uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center justify-between';
        darkHeader.innerHTML = `<span><i class="fas fa-moon mr-1.5 text-indigo-400"></i> Tema Gelap / Dark Mode (${darkThemes.length})</span><span class="text-[9px] bg-slate-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-slate-500">Eye-Friendly</span>`;
        grid.appendChild(darkHeader);

        darkThemes.forEach(theme => {
            grid.appendChild(this.createThemeCard(theme));
        });

        // Light Section Header
        const lightHeader = document.createElement('div');
        lightHeader.className = 'px-3 py-1.5 mt-2 text-[10px] font-black uppercase tracking-wider text-slate-400 dark:text-slate-500 flex items-center justify-between border-t border-slate-100 dark:border-slate-800 pt-2';
        lightHeader.innerHTML = `<span><i class="fas fa-sun mr-1.5 text-amber-400"></i> Tema Terang / Light Mode (${lightThemes.length})</span><span class="text-[9px] bg-slate-100 dark:bg-slate-800 px-1.5 py-0.5 rounded text-slate-500">Anti-Silau</span>`;
        grid.appendChild(lightHeader);

        lightThemes.forEach(theme => {
            grid.appendChild(this.createThemeCard(theme));
        });
    },

    createThemeCard: function (theme) {
        const isSelected = theme.id === this.currentTheme;
        const card = document.createElement('div');
        card.className = `flex items-center justify-between p-2 rounded-2xl cursor-pointer transition-all ${
            isSelected
                ? 'bg-indigo-50/90 dark:bg-indigo-950/60 border-2 border-indigo-500 shadow-sm'
                : 'hover:bg-slate-50 dark:hover:bg-slate-800/80 border border-slate-100 dark:border-slate-800/60'
        }`;

        card.onclick = () => {
            this.applyTheme(theme.id);
            this.closeDropdown();
        };

        const swatches = theme.colors.map(c =>
            `<span class="w-3.5 h-3.5 rounded-full shadow-2xs border border-white/20 inline-block" style="background-color: ${c}"></span>`
        ).join('');

        card.innerHTML = `
            <div class="flex items-center gap-2.5 min-w-0 flex-1">
                <div class="w-7 h-7 rounded-xl flex items-center justify-center text-xs flex-shrink-0 shadow-xs" style="background: linear-gradient(135deg, ${theme.colors[0]}, ${theme.colors[1]}); color: white;">
                    <i class="fas ${theme.icon}"></i>
                </div>
                <div class="min-w-0 flex-1">
                    <div class="flex items-center gap-1.5">
                        <span class="text-xs font-bold text-slate-800 dark:text-white truncate">${theme.name}</span>
                        <span class="text-[9px] font-bold px-1.5 py-0.2 rounded-full ${theme.category === 'dark' ? 'bg-slate-800 text-slate-200 border border-slate-700' : 'bg-slate-100 text-slate-600'}">${theme.tag}</span>
                    </div>
                    <p class="text-[10px] text-slate-400 dark:text-slate-400 truncate">${theme.desc}</p>
                </div>
            </div>
            <div class="flex items-center gap-1 pl-2 flex-shrink-0">
                ${swatches}
                ${isSelected ? '<i class="fas fa-check-circle text-indigo-500 text-xs ml-1"></i>' : ''}
            </div>
        `;

        return card;
    },

    // ── FONT MANAGEMENT (5 GOOGLE FONTS) ──────────────────────────
    applyFont: function (fontId) {
        const font = FONTS_CONFIG.find(f => f.id === fontId) || FONTS_CONFIG[0];
        this.currentFont = font.id;
        document.documentElement.setAttribute('data-font', font.id);
        localStorage.setItem('app_font', font.id);

        // Update active badge in configuration page if open
        const fontCards = document.querySelectorAll('.font-preview-card');
        fontCards.forEach(card => {
            const cardFontId = card.getAttribute('data-font-id');
            if (cardFontId === font.id) {
                card.classList.add('ring-2', 'ring-indigo-600', 'bg-indigo-50/60', 'dark:bg-indigo-950/40');
                card.classList.remove('border-slate-200', 'dark:border-slate-700');
                const badge = card.querySelector('.font-active-indicator');
                if (badge) badge.classList.remove('hidden');
            } else {
                card.classList.remove('ring-2', 'ring-indigo-600', 'bg-indigo-50/60', 'dark:bg-indigo-950/40');
                card.classList.add('border-slate-200', 'dark:border-slate-700');
                const badge = card.querySelector('.font-active-indicator');
                if (badge) badge.classList.add('hidden');
            }
        });
    },

    setGlobalFont: function (fontId, showSuccessToast) {
        this.applyFont(fontId);
        
        // Post to server if possible
        fetch('/Configuration/UpdateGlobalFont', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded',
            },
            body: `fontName=${encodeURIComponent(fontId)}`
        }).catch(err => console.log('Font persisted locally:', err));

        if (showSuccessToast && typeof Swal !== 'undefined') {
            const font = FONTS_CONFIG.find(f => f.id === fontId) || FONTS_CONFIG[0];
            Swal.fire({
                icon: 'success',
                title: 'Font Berhasil Diterapkan!',
                text: `Tipografi global sekarang menggunakan ${font.name}.`,
                confirmButtonColor: '#6366F1',
                timer: 2000,
                timerProgressBar: true
            });
        }
    }
};

window.ThemeManager = ThemeManager;
window.FONTS_CONFIG = FONTS_CONFIG;

// Auto initialize on DOMContentLoaded
document.addEventListener('DOMContentLoaded', function () {
    ThemeManager.init();
});
