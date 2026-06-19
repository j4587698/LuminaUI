import { dotnet } from './_framework/dotnet.js';

const isBrowser = typeof window !== 'undefined';
if (!isBrowser) {
    throw new Error('Expected to be running in a browser.');
}

const supportedCultures = ['en-US', 'zh-CN'];

function normalizeCulture(culture) {
    if (!culture) {
        return 'en-US';
    }

    const normalized = culture.replace('_', '-');
    const exact = supportedCultures.find(item => item.toLowerCase() === normalized.toLowerCase());
    if (exact) {
        return exact;
    }

    return normalized.toLowerCase().startsWith('zh') ? 'zh-CN' : 'en-US';
}

function getStartupCulture() {
    const queryCulture = new URL(globalThis.location.href).searchParams.get('culture');
    if (queryCulture) {
        return normalizeCulture(queryCulture);
    }

    const languages = globalThis.navigator?.languages;
    if (languages && languages.length > 0) {
        return normalizeCulture(languages[0]);
    }

    return normalizeCulture(globalThis.navigator?.language);
}

const startupCulture = getStartupCulture();

const dotnetRuntime = await dotnet
    .withDiagnosticTracing(false)
    .withApplicationCulture(startupCulture)
    .withConfig({ loadAllSatelliteResources: true })
    .withApplicationArgumentsFromQuery()
    .create();

const config = dotnetRuntime.getConfig();

await dotnetRuntime.runMain(config.mainAssemblyName, [globalThis.location.href, startupCulture]);
