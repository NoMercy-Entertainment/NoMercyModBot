// @ts-nocheck
/* eslint-disable */

import { UAParser } from 'ua-parser-js';

export interface UUID {
	agent: string;
	parser: Parser;
	screen: Screen;
	browser: Browser;
	device: AppAgent | null,
	cpu: CPU;
	os: OS;
	get: Get;
}

export interface Browser {
	language: string;
	name: string;
	version: string;
	major: string;
	engine: string;
	arch: string;
	vendor: string;
}

export interface CPU {
	arch: string;
	cores: number;
}

export interface Get {
	os: OS;
	cpu: CPU;
	browser: Browser;
	screen: Screen;
	deviceId: string,
	device: AppAgent | null,
}

export interface OS {
	vendor: string;
	name: string;
	version: string;
}

export interface Screen {
	pixelDepth: number;
	// orientation: Orientation;
	width: number;
	height: number;
	isTouchScreen: boolean;
}

export interface AppAgent {
	android_version?: number,
	brand?: string,
	model?: string,
	name?: string
}

// export interface Orientation {
// 	type: string;
// 	angle: number;
// }

export interface Parser {
}

const createUUID = () => {
	class UID {
		agent: string = navigator.userAgent.replace(/^\s*/u, '').replace(/\s*$/u, '');
		parser = new UAParser();
		screen: Screen = {
			pixelDepth: screen.pixelDepth,
			// orientation: {
			// 	type: screen.orientation.type,
			// 	angle: screen.orientation.angle,
			// },
			width: screen.availWidth,
			height: screen.availHeight,
			isTouchScreen:
				'ontouchstart' in window ||
				navigator.maxTouchPoints > 0 ||
				// @ts-ignore
				navigator.msMaxTouchPoints > 0,
		};

		browser: Browser = {
			language: (
				navigator.language ||
				// @ts-ignore
				navigator.userLanguage ||
				// @ts-ignore
				navigator.browserLanguage ||
				// @ts-ignore
				navigator.systemLanguage ||
				''
			).toLowerCase(),
			engine: this.parser.getEngine().name ?? '',
			arch: navigator.platform,
			vendor: navigator.vendor,
			// @ts-ignore
			name: navigator.userAgentData?.brands?.[navigator.userAgentData?.brands.length - 2]?.brand
				// @ts-ignore
				?? navigator.userAgentData?.brands?.[navigator.userAgentData?.brands.length - 1]?.brand
				?? this.getPlatform(),
			...this.parser.getBrowser(),
			version: this.parser.getBrowser().version ?? '',
			major: this.parser.getBrowser().major ?? '',
		};

		cpu: CPU = {
			arch: this.parser.getCPU().architecture ?? '',
			cores: navigator.hardwareConcurrency,
		};

		os: OS = {
			vendor: this.getPlatform(),
			...this.parser.getOS(),
			name: this.getPlatform(),
			version: this.parser.getOS().version ?? '',
		};

		device: AppAgent = {
			android_version: undefined,
			brand: undefined,
			model: undefined,
			name: undefined,
			...this.getAppDetails(),
		};
		deviceId: string = (() => {
			const variant = 'b';
			const tmpUuid = this.hashMD5(
				[
					location.origin,
					this.os.name,
					this.os.vendor,
					this.os.version,
					this.cpu.arch,
					this.cpu.cores,
					this.browser.arch,
					this.browser.engine,
					this.browser.language,
					this.browser.major,
					this.browser.name,
					this.browser.vendor,
					this.browser.version,
					this.screen.isTouchScreen,
				].join(':'),
			);

			return [
				tmpUuid.slice(0, 8),
				tmpUuid.slice(8, 12),
				`3${tmpUuid.slice(12, 15)}`,
				variant + tmpUuid.slice(15, 18),
				tmpUuid.slice(20),
			].join('-');
		})();

		hashMD5(s: string) {
			const L = (k: number, d: number) => (k << d) | (k >>> (32 - d));
			// @ts-ignore
			const K = (G: number, k: number) => {
				const F: number = G & 2147483648;
				const H: number = k & 2147483648;
				const I: number = G & 1073741824;
				const d: number = k & 1073741824;
				const x: number = (G & 1073741823) + (k & 1073741823);
				if (I & d) {
					return x ^ 2147483648 ^ F ^ H;
				}
				if (I | d) {
					if (x & 1073741824) {
						return x ^ 3221225472 ^ F ^ H;
					}
					return x ^ 1073741824 ^ F ^ H;
				}
				return x ^ F ^ H;
			};
			const r = (d: number, F: number, k: number) => (d & F) | (~d & k);
			const q = (d: number, F: number, k: number) => (d & k) | (F & ~k);
			const p = (d: number, F: number, k: number) => d ^ F ^ k;
			const n = (d: number, F: number, k: number) => F ^ (d | ~k);
			const u = (G: number, F: any, aa: any, Z: any, k: any, H: number, I: number) => {
				G = K(G, K(K(r(F, aa, Z), k), I));
				return K(L(G, H), F);
			};
			const f = (G: number, F: any, aa: any, Z: any, k: any, H: number, I: number) => {
				G = K(G, K(K(q(F, aa, Z), k), I));
				return K(L(G, H), F);
			};
			const D = (G: number, F: any, aa: any, Z: any, k: any, H: number, I: number) => {
				G = K(G, K(K(p(F, aa, Z), k), I));
				return K(L(G, H), F);
			};
			const t = (G: number, F: any, aa: any, Z: any, k: any, H: number, I: number) => {
				G = K(G, K(K(n(F, aa, Z), k), I));
				return K(L(G, H), F);
			};
			const e = (G: string) => {
				const F = G.length;
				const x = F + 8;
				const k = (x - (x % 64)) / 64;
				const I = (k + 1) * 16;
				const aa = [I - 1];
				let d: number;
				let H = 0;
				while (H < F) {
					const Z = (H - (H % 4)) / 4;
					const d = (H % 4) * 8;
					aa[Z] = aa[Z] | (G.charCodeAt(H) << d);
					H++;
				}
				const Z: number = (H - (H % 4)) / 4;
				d = (H % 4) * 8;
				aa[Z] = aa[Z] | (128 << d);
				aa[I - 2] = F << 3;
				aa[I - 1] = F >>> 29;
				return aa;
			};
			// @ts-ignore
			const B = (x: number) => {
				let k = '';
				let F = '';
				let G: number;
				let d: number;
				for (d = 0; d <= 3; d++) {
					G = (x >>> (d * 8)) & 255;
					F = `0${G.toString(16)}`;
					k += F.substr(F.length - 2, 2);
				}
				return k;
			};
			// @ts-ignore
			const J = (k: string) => {
				k = k.replace(/rn/g, 'n');
				let d = '';
				for (let F = 0; F < k.length; F++) {
					const x = k.charCodeAt(F);
					if (x < 128) {
						d += String.fromCharCode(x);
					} else if (x > 127 && x < 2048) {
						d += String.fromCharCode((x >> 6) | 192);
						d += String.fromCharCode((x & 63) | 128);
					} else {
						d += String.fromCharCode((x >> 12) | 224);
						d += String.fromCharCode(((x >> 6) & 63) | 128);
						d += String.fromCharCode((x & 63) | 128);
					}
				}
				return d;
			};
			let C: any[];
			let P: number;
			let h: any;
			let E: any;
			let v: any;
			let g: any;
			let Y: number;
			let X: number;
			let W: number;
			let V: number;
			const S = 7;
			const Q = 12;
			const N = 17;
			const M = 22;
			const A = 5;
			const z = 9;
			const y = 14;
			const w = 20;
			const o = 4;
			const m = 11;
			const l = 16;
			const j = 23;
			const U = 6;
			const T = 10;
			const R = 15;
			const O = 21;
			s = J(s);
			// @ts-ignore
			C = e(s);
			Y = 1732584193;
			X = 4023233417;
			W = 2562383102;
			V = 271733878;
			for (let P = 0; P < C.length; P += 16) {
				h = Y;
				E = X;
				v = W;
				g = V;
				Y = u(Y, X, W, V, C[P + 0], S, 3614090360);
				V = u(V, Y, X, W, C[P + 1], Q, 3905402710);
				W = u(W, V, Y, X, C[P + 2], N, 606105819);
				X = u(X, W, V, Y, C[P + 3], M, 3250441966);
				Y = u(Y, X, W, V, C[P + 4], S, 4118548399);
				V = u(V, Y, X, W, C[P + 5], Q, 1200080426);
				W = u(W, V, Y, X, C[P + 6], N, 2821735955);
				X = u(X, W, V, Y, C[P + 7], M, 4249261313);
				Y = u(Y, X, W, V, C[P + 8], S, 1770035416);
				V = u(V, Y, X, W, C[P + 9], Q, 2336552879);
				W = u(W, V, Y, X, C[P + 10], N, 4294925233);
				X = u(X, W, V, Y, C[P + 11], M, 2304563134);
				Y = u(Y, X, W, V, C[P + 12], S, 1804603682);
				V = u(V, Y, X, W, C[P + 13], Q, 4254626195);
				W = u(W, V, Y, X, C[P + 14], N, 2792965006);
				X = u(X, W, V, Y, C[P + 15], M, 1236535329);
				Y = f(Y, X, W, V, C[P + 1], A, 4129170786);
				V = f(V, Y, X, W, C[P + 6], z, 3225465664);
				W = f(W, V, Y, X, C[P + 11], y, 643717713);
				X = f(X, W, V, Y, C[P + 0], w, 3921069994);
				Y = f(Y, X, W, V, C[P + 5], A, 3593408605);
				V = f(V, Y, X, W, C[P + 10], z, 38016083);
				W = f(W, V, Y, X, C[P + 15], y, 3634488961);
				X = f(X, W, V, Y, C[P + 4], w, 3889429448);
				Y = f(Y, X, W, V, C[P + 9], A, 568446438);
				V = f(V, Y, X, W, C[P + 14], z, 3275163606);
				W = f(W, V, Y, X, C[P + 3], y, 4107603335);
				X = f(X, W, V, Y, C[P + 8], w, 1163531501);
				Y = f(Y, X, W, V, C[P + 13], A, 2850285829);
				V = f(V, Y, X, W, C[P + 2], z, 4243563512);
				W = f(W, V, Y, X, C[P + 7], y, 1735328473);
				X = f(X, W, V, Y, C[P + 12], w, 2368359562);
				Y = D(Y, X, W, V, C[P + 5], o, 4294588738);
				V = D(V, Y, X, W, C[P + 8], m, 2272392833);
				W = D(W, V, Y, X, C[P + 11], l, 1839030562);
				X = D(X, W, V, Y, C[P + 14], j, 4259657740);
				Y = D(Y, X, W, V, C[P + 1], o, 2763975236);
				V = D(V, Y, X, W, C[P + 4], m, 1272893353);
				W = D(W, V, Y, X, C[P + 7], l, 4139469664);
				X = D(X, W, V, Y, C[P + 10], j, 3200236656);
				Y = D(Y, X, W, V, C[P + 13], o, 681279174);
				V = D(V, Y, X, W, C[P + 0], m, 3936430074);
				W = D(W, V, Y, X, C[P + 3], l, 3572445317);
				X = D(X, W, V, Y, C[P + 6], j, 76029189);
				Y = D(Y, X, W, V, C[P + 9], o, 3654602809);
				V = D(V, Y, X, W, C[P + 12], m, 3873151461);
				W = D(W, V, Y, X, C[P + 15], l, 530742520);
				X = D(X, W, V, Y, C[P + 2], j, 3299628645);
				Y = t(Y, X, W, V, C[P + 0], U, 4096336452);
				V = t(V, Y, X, W, C[P + 7], T, 1126891415);
				W = t(W, V, Y, X, C[P + 14], R, 2878612391);
				X = t(X, W, V, Y, C[P + 5], O, 4237533241);
				Y = t(Y, X, W, V, C[P + 12], U, 1700485571);
				V = t(V, Y, X, W, C[P + 3], T, 2399980690);
				W = t(W, V, Y, X, C[P + 10], R, 4293915773);
				X = t(X, W, V, Y, C[P + 1], O, 2240044497);
				Y = t(Y, X, W, V, C[P + 8], U, 1873313359);
				V = t(V, Y, X, W, C[P + 15], T, 4264355552);
				W = t(W, V, Y, X, C[P + 6], R, 2734768916);
				X = t(X, W, V, Y, C[P + 13], O, 1309151649);
				Y = t(Y, X, W, V, C[P + 4], U, 4149444226);
				V = t(V, Y, X, W, C[P + 11], T, 3174756917);
				W = t(W, V, Y, X, C[P + 2], R, 718787259);
				X = t(X, W, V, Y, C[P + 9], O, 3951481745);
				Y = K(Y, h);
				X = K(X, E);
				W = K(W, v);
				V = K(V, g);
			}
			const i = B(Y) + B(X) + B(W) + B(V);
			return i.toLowerCase();
		}

		getAppDetails() {
			const regex = /Android\s(?<version>\d{1,2});\sNoMercy\sTV;\s(?<name>.+);\s(?<brand>.+);\s(?<model>.+)/iu;
			if (regex.test(navigator.userAgent)) {

				// @ts-ignore
				const groups = regex.exec(navigator.userAgent)?.groups;
				if (!groups) return null;

				return {
					android_version: parseInt(groups.android_version, 10),
					brand: groups.brand,
					model: groups.model,
					name: groups.name,
				};
			}

			return null;
		}

		getPlatform() {
			const ua = navigator.userAgent.replace(/^\s*/, '').replace(/\s*$/, '');
			const Agent = {
				isMac: false,
				isiPad: false,
				isiPod: false,
				isiPhone: false,
				isAndroid: false,
				isAndroidTv: false,
				isBlackberry: false,
				isFirefox: false,
				isWindowsPhone: false,
				isSamsung: false,
				isLinux: false,
				isCurl: false,
			};
			const _Platform = {
				Firefox: /Firefox/i,
				Windows: /windows nt/i,
				WindowsPhone: /windows phone/i,
				Mac: /macintosh/i,
				Linux: /linux/i,
				Wii: /wii/i,
				Playstation: /playstation/i,
				iPad: /ipad/i,
				iPod: /ipod/i,
				iPhone: /iphone/i,
				Android: /android/i,
				Blackberry: /blackberry/i,
				Samsung: /samsung/i,
				Curl: /curl/i,
				AndroidTv: /Android\s(?<version>\d{1,2});\s/gm,
			};
			switch (true) {
				case _Platform.Wii.test(ua):
					return 'Wii';
				case _Platform.Playstation.test(ua):
					return 'Playstation';
				case _Platform.iPad.test(ua):
					Agent.isiPad = true;
					return 'iPad';
				case _Platform.iPod.test(ua):
					Agent.isiPod = true;
					return 'iPod';
				case _Platform.iPhone.test(ua):
					Agent.isiPhone = true;
					return 'iPhone';
				case _Platform.Samsung.test(ua):
					Agent.isSamsung = true;
					return 'Samsung';
				case _Platform.Mac.test(ua):
					Agent.isMac = true;
					return 'Apple';
				case _Platform.Curl.test(ua):
					Agent.isCurl = true;
					return 'Curl';
				case _Platform.Blackberry.test(ua):
					Agent.isBlackberry = true;
					return 'Blackberry';
				case _Platform.Firefox.test(ua):
					Agent.isFirefox = true;
					return 'Firefox';
				case _Platform.Windows.test(ua):
					return 'Microsoft';
				case _Platform.WindowsPhone.test(ua):
					Agent.isWindowsPhone = true;
					return 'Microsoft Windows Phone';
				case _Platform.AndroidTv.test(ua):
					Agent.isAndroidTv = true;
					return 'Android TV';
				case _Platform.Android.test(ua):
					Agent.isAndroid = true;
					return 'Android';
				case _Platform.Linux.test(ua):
					Agent.isLinux = true;
					return 'Linux';
				default:
					return 'unknown';
			}
		}
	}

	const uid = new UID();

	return {
		os: uid.os,
		cpu: uid.cpu,
		browser: uid.browser,
		screen: uid.screen,
		deviceId: uid.deviceId,
		device: uid.device,
	};
};

export default createUUID;
