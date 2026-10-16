/** Keeps one streamed source stable while the mix catalogue is refreshed. */
export class MixPlayback {
    private request = 0;
    private fileId: string | null = null;
    private readonly audio: HTMLAudioElement;
    private readonly getUrl: (fileId: string) => Promise<string>;

    constructor(
        audio: HTMLAudioElement,
        getUrl: (fileId: string) => Promise<string>,
    ) {
        this.audio = audio;
        this.getUrl = getUrl;
    }

    async play(fileId: string): Promise<boolean> {
        const request = ++this.request;
        if (this.fileId !== fileId || !this.audio.hasAttribute("src") || this.audio.error) {
            this.release();
            this.fileId = fileId;
            try {
                const url = await this.getUrl(fileId);
                if (request !== this.request) return false;
                this.audio.src = url;
            } catch (error) {
                if (request !== this.request) return false;
                throw error;
            }
        }
        try {
            await this.audio.play();
            return request === this.request;
        } catch (error) {
            if (request !== this.request) return false;
            throw error;
        }
    }

    stop() {
        ++this.request;
        this.fileId = null;
        this.release();
    }

    private release() {
        this.audio.pause();
        this.audio.removeAttribute("src");
        this.audio.load();
    }
}
