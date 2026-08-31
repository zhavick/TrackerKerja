/**
 * ════════════════════════════════════════════════════════════════════════════════
 * WORK TRACKER PRO — DYNAMIC THEME MANAGER (16 THEMES: 10 LIGHT & 6 DARK)
 * ════════════════════════════════════════════════════════════════════════════════
 */

const THEMES_CONFIG = [
    // ── DARK THEMES (6 THEMES) ──────────────────────────────────────────────────
    {
        id: "dark-midnight",
        name: "Midnight OLED",
        desc: "Hitam karbon OLED pekat & neon indigo",
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

    // ── LIGHT THEMES (10 THEMES) ─────────────────────────────────────────────────
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
        desc: "Fuchsia neon & cyan futuristik",
        icon: "fa-bolt",
        colors: ["#D946EF", "#06B6D4", "#FFFFFF"],
        tag: "Vibrant",
        category: "light"
    },
    {
        id: "amethyst",
        name: "Royal Amethyst",
        desc: "Ungu kerajaan mewah & berkelas",
        icon: "fa-gem",
        colors: ["#9333EA", "#6366F1", "#FFFFFF"],
        tag: "Luxury",
        category: "light"
    },
    {
        id: "amber",
        name: "Amber Gold",
        desc: "Emas hangat berkilau & optimis",
        icon: "fa-fire",
        colors: ["#F59E0B", "#EA580C", "#FFFFFF"],
        tag: "Golden",
        category: "light"
    },
    {
        id: "slate",
        name: "Slate Minimalist",
        desc: "Monokrom minimalis & bersih",
        icon: "fa-cube",
        colors: ["#475569", "#334155", "#FFFFFF"],
        tag: "Minimal",
        category: "light"
    },
    {
        id: "teal",
        name: "Nordic Teal",
        desc: "Toska menyegarkan & dinamis",
        icon: "fa-compass",
        colors: ["#0D9488", "#0284C7", "#FFFFFF"],
        tag: "Fresh",
        category: "light"
    },
    {
        id: "midnight",
        name: "Midnight Titanium",
        desc: "Titanium cerah aksen royal indigo",
        icon: "fa-shield",
        colors: ["#6366F1", "#38BDF8", "#FFFFFF"],
        tag: "Titanium",
        category: "light"
    }
];

const ThemeManager = {
    storageKey: "app_theme",
    currentFilter: "all",

    getTheme() {
        return localStorage.getItem(this.storageKey) || "indigo";
    },

    setTheme(themeId, showToast = true) {
        const theme = THEMES_CONFIG.find(t => t.id === themeId) || THEMES_CONFIG[0];
        document.documentElement.setAttribute("data-theme", theme.id);
        localStorage.setItem(this.storageKey, theme.id);

        // Update active checkmarks in topbar dropdown & profile page
        this.updateUIActiveState(theme.id);

        if (showToast) {
            this.showThemeToast(theme);
        }
    },

    updateUIActiveState(activeId) {
        // Update topbar trigger label & icon swatch
        const topbarLabel = document.getElementById("topbar-theme-label");
        const topbarSwatch = document.getElementById("topbar-theme-swatch");
        const currentTheme = THEMES_CONFIG.find(t => t.id === activeId) || THEMES_CONFIG[0];

        if (topbarLabel) {
            topbarLabel.textContent = currentTheme.name;
        }
        if (topbarSwatch) {
            topbarSwatch.style.background = currentTheme.colors[0];
        }

        // Update checkmarks and active rings on all theme buttons
        document.querySelectorAll("[data-theme-choice]").forEach(el => {
            const elId = el.getAttribute("data-theme-choice");
            const isMatch = elId === activeId;
            const checkIcon = el.querySelector(".theme-check-icon");
            
            if (isMatch) {
                el.classList.add("ring-2", "ring-indigo-500", "bg-indigo-50/50", "border-indigo-400");
                el.classList.remove("border-slate-200");
                if (checkIcon) checkIcon.classList.remove("hidden");
            } else {
                el.classList.remove("ring-2", "ring-indigo-500", "bg-indigo-50/50", "border-indigo-400");
                el.classList.add("border-slate-200");
                if (checkIcon) checkIcon.classList.add("hidden");
            }
        });
    },

    toggleDropdown() {
        const dropdown = document.getElementById("themeDropdownMenu");
        if (!dropdown) return;

        if (dropdown.classList.contains("hidden")) {
            // Close other open dropdowns
            if (typeof closeUserProfileDropdown === "function") closeUserProfileDropdown();
            const notifMenu = document.getElementById("notifDropdownMenu");
            if (notifMenu) notifMenu.classList.add("hidden");

            dropdown.classList.remove("hidden");
            this.renderDropdownContent();
            // Close when clicking outside
            setTimeout(() => {
                window.addEventListener("click", this.onOutsideClick);
            }, 10);
        } else {
            this.closeDropdown();
        }
    },

    closeDropdown() {
        const dropdown = document.getElementById("themeDropdownMenu");
        if (dropdown) {
            dropdown.classList.add("hidden");
        }
        window.removeEventListener("click", this.onOutsideClick);
    },

    onOutsideClick(e) {
        const dropdown = document.getElementById("themeDropdownMenu");
        const btn = document.getElementById("themeDropdownBtn");
        if (dropdown && !dropdown.contains(e.target) && btn && !btn.contains(e.target)) {
            ThemeManager.closeDropdown();
        }
    },

    filterThemes(cat) {
        this.currentFilter = cat;
        this.renderDropdownContent();
    },

    renderDropdownContent() {
        const container = document.getElementById("themeDropdownGrid");
        if (!container) return;

        const currentThemeId = this.getTheme();
        const darkThemes = THEMES_CONFIG.filter(t => t.category === "dark");
        const lightThemes = THEMES_CONFIG.filter(t => t.category === "light");

        let html = `
            <!-- Category Tabs -->
            <div class="flex items-center gap-1 bg-slate-100 p-1 rounded-xl mb-2.5 text-xs font-bold">
                <button type="button" onclick="ThemeManager.filterThemes('all')" class="flex-1 py-1 px-2 rounded-lg ${this.currentFilter === 'all' ? 'bg-white text-indigo-700 shadow-2xs' : 'text-slate-500 hover:text-slate-800'} transition-all text-center text-[11px]">
                    Semua (${THEMES_CONFIG.length})
                </button>
                <button type="button" onclick="ThemeManager.filterThemes('dark')" class="flex-1 py-1 px-2 rounded-lg ${this.currentFilter === 'dark' ? 'bg-slate-900 text-white shadow-2xs' : 'text-slate-500 hover:text-slate-800'} transition-all text-center text-[11px] flex items-center justify-center gap-1">
                    <i class="fas fa-moon text-amber-300 text-[10px]"></i> Gelap (${darkThemes.length})
                </button>
                <button type="button" onclick="ThemeManager.filterThemes('light')" class="flex-1 py-1 px-2 rounded-lg ${this.currentFilter === 'light' ? 'bg-white text-amber-600 shadow-2xs' : 'text-slate-500 hover:text-slate-800'} transition-all text-center text-[11px] flex items-center justify-center gap-1">
                    <i class="fas fa-sun text-amber-500 text-[10px]"></i> Terang (${lightThemes.length})
                </button>
            </div>
            <div class="space-y-1.5 max-h-[340px] overflow-y-auto pr-1">
        `;

        const listToRender = this.currentFilter === "dark" ? darkThemes :
                             this.currentFilter === "light" ? lightThemes : THEMES_CONFIG;

        listToRender.forEach(t => {
            const isActive = t.id === currentThemeId;
            const isDark = t.category === "dark";
            html += `
                <button type="button" onclick="ThemeManager.setTheme('${t.id}')" data-theme-choice="${t.id}"
                        class="w-full text-left p-2.5 rounded-2xl border ${isActive ? 'ring-2 ring-indigo-500 bg-indigo-50/50 border-indigo-400' : 'border-slate-100 hover:border-indigo-200 hover:bg-slate-50'} transition-all flex items-center justify-between group">
                    <div class="flex items-center gap-3">
                        <div class="flex items-center -space-x-1.5 flex-shrink-0">
                            <span class="w-4 h-4 rounded-full border-2 border-white shadow-xs" style="background: ${t.colors[0]}"></span>
                            <span class="w-4 h-4 rounded-full border-2 border-white shadow-xs" style="background: ${t.colors[1]}"></span>
                            <span class="w-4 h-4 rounded-full border-2 border-white shadow-xs" style="background: ${t.colors[2]}"></span>
                        </div>
                        <div>
                            <div class="flex items-center gap-1.5">
                                <span class="text-xs font-bold text-slate-800 group-hover:text-indigo-600 transition-colors">${t.name}</span>
                                <span class="text-[9px] px-1.5 py-0.2 rounded-md ${isDark ? 'bg-slate-900 text-amber-300 border border-slate-700' : 'bg-slate-100 text-slate-600'} font-bold">
                                    ${isDark ? '<i class="fas fa-moon text-[8px] mr-0.5"></i>' : ''}${t.tag}
                                </span>
                            </div>
                            <p class="text-[10px] text-slate-400 truncate max-w-[170px]">${t.desc}</p>
                        </div>
                    </div>
                    <div class="theme-check-icon ${isActive ? '' : 'hidden'} text-indigo-600 font-black text-xs">
                        <i class="fas fa-check-circle"></i>
                    </div>
                </button>
            `;
        });

        html += `</div>`;
        container.innerHTML = html;
    },

    renderProfileThemeCards(containerId) {
        const container = document.getElementById(containerId);
        if (!container) return;

        const currentThemeId = this.getTheme();

        container.innerHTML = THEMES_CONFIG.map(t => {
            const isActive = t.id === currentThemeId;
            const isDark = t.category === "dark";
            return `
                <div onclick="ThemeManager.setTheme('${t.id}')" data-theme-choice="${t.id}"
                     class="cursor-pointer p-4 rounded-2xl border-2 ${isActive ? 'ring-2 ring-indigo-500 bg-indigo-50/40 border-indigo-500 shadow-md' : 'border-slate-200 hover:border-indigo-300 hover:bg-slate-50/70'} transition-all flex flex-col justify-between group relative overflow-hidden">
                    <div class="flex items-center justify-between mb-3">
                        <div class="w-8 h-8 rounded-xl flex items-center justify-center text-white text-xs shadow-md" style="background: ${t.colors[0]}">
                            <i class="fas ${t.icon}"></i>
                        </div>
                        <span class="text-[10px] font-bold px-2 py-0.5 rounded-lg ${isDark ? 'bg-slate-900 text-amber-300 border border-slate-700' : 'bg-slate-100 text-slate-600'}">
                            ${isDark ? '🌙 ' : ''}${t.tag}
                        </span>
                    </div>

                    <!-- Swatches Bar -->
                    <div class="h-3 w-full rounded-lg overflow-hidden flex mb-2.5 shadow-inner">
                        <div class="flex-1" style="background: ${t.colors[0]}"></div>
                        <div class="flex-1" style="background: ${t.colors[1]}"></div>
                        <div class="flex-1" style="background: ${t.colors[2]}"></div>
                    </div>

                    <div>
                        <div class="flex items-center justify-between">
                            <h4 class="text-xs font-black text-slate-800 group-hover:text-indigo-600 transition-colors">${t.name}</h4>
                            <div class="theme-check-icon ${isActive ? '' : 'hidden'} text-indigo-600 text-sm">
                                <i class="fas fa-check-circle"></i>
                            </div>
                        </div>
                        <p class="text-[11px] text-slate-400 mt-0.5 line-clamp-1">${t.desc}</p>
                    </div>
                </div>
            `;
        }).join("");
    },

    showThemeToast(theme) {
        let toast = document.getElementById("theme-toast");
        if (!toast) {
            toast = document.createElement("div");
            toast.id = "theme-toast";
            toast.className = "fixed bottom-5 right-5 z-[9999] bg-slate-900 text-white px-4 py-3 rounded-2xl shadow-2xl border border-slate-700 flex items-center gap-3 transition-all duration-300 transform translate-y-20 opacity-0";
            document.body.appendChild(toast);
        }

        toast.innerHTML = `
            <div class="w-7 h-7 rounded-xl flex items-center justify-center text-white text-xs shadow-sm flex-shrink-0" style="background: ${theme.colors[0]}">
                <i class="fas ${theme.icon}"></i>
            </div>
            <div>
                <p class="text-xs font-bold text-white leading-tight">Tema ${theme.name} Diaktifkan!</p>
                <p class="text-[10px] text-slate-400">Mode ${theme.category === 'dark' ? 'Gelap (Dark)' : 'Terang (Light)'} telah disimpan.</p>
            </div>
        `;

        toast.classList.remove("translate-y-20", "opacity-0");
        toast.classList.add("translate-y-0", "opacity-100");

        if (this._toastTimer) clearTimeout(this._toastTimer);
        this._toastTimer = setTimeout(() => {
            toast.classList.remove("translate-y-0", "opacity-100");
            toast.classList.add("translate-y-20", "opacity-0");
        }, 2500);
    }
};

// Initial run when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
    const active = ThemeManager.getTheme();
    ThemeManager.setTheme(active, false);
});
