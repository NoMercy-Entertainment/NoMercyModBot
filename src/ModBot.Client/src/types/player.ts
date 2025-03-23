import type { Ref } from 'vue';

export interface PlayerEmbed {
  config: ChannelEmbedConfig,
  element?: Ref<HTMLElement | null>,
  elementId: string,
  qualities: string[],
  quality: string;
  playbackStats?: PlaybackStats;
}

export interface ChannelEmbedConfig {
  width: string,
  height: string,
  channel: string,
  volume: number,
  parent: string[],
}

export interface PlaybackStats {
  backendVersion: string,
  bufferSize: number,
  codecs: string,
  displayResolution: string,
  fps: number,
  hlsLatencyBroadcaster: number,
  playbackRate: number,
  skippedFrames: number,
  videoResolution: string,
  name: string,
}
