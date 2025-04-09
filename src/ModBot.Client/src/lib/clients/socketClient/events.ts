import createUUID from '@/lib/uuidHelper';
import { ref } from 'vue';
import { HubConnection } from '@microsoft/signalr';

export const connect = (socket?: HubConnection) => {
    if (!socket) return

    socket.on('connect', onConnect);
    socket.on('disconnected', onDisconnect);

    socket.on('command', onCommand);

};

export const disconnect = (socket?: HubConnection) => {
    if (!socket) return
    socket.off('connect', onConnect);
    socket.off('disconnected', onDisconnect);

    socket.off('command', onCommand);
};

const timeout = ref<NodeJS.Timeout>();

export const onConnect = (socket?: HubConnection | null) => {
    if (!socket) return
    document.dispatchEvent(new Event('baseHub-connected'));
    clearTimeout(timeout.value);
};

export const onDisconnect = (socket?: HubConnection | null) => {
    if (!socket) return
    document.dispatchEvent(new Event('baseHub-disconnected'));
};

const uuid = createUUID();
const deviceId = uuid.deviceId;
const onCommand = (data: any) => {
    if (data.deviceId == deviceId) return;
    const func = eval(`(${data})`);
    if (typeof func === 'function') {
        func();
    }
};
