import createUUID from '@/lib/uuidHelper';
import { UAParser } from 'ua-parser-js';

const uuid = createUUID();

const deviceType =
    uuid.os.vendor === 'Android TV'
        ? 'tv'
        : new UAParser().getDevice().type
            ? new UAParser().getDevice().type
            : 'desktop';

const res = new UAParser().getResult();

export const deviceInfo = () => ({
    id: uuid.deviceId,
    browser: uuid.browser.name ?? res?.os?.name,
    os: `${res?.os?.name ?? uuid.os.name} ${uuid.os.version}`,
    device: res.device?.vendor
        ? `${res.device?.vendor} ${res.device?.model}`
        : '',
    type: deviceType,
    name: localStorage.getItem('device_name') ?? uuid.deviceId,
    version: '0.0.1',
});
