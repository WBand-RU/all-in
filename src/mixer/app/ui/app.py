import threading
import tkinter as tk
from tkinter import filedialog, messagebox
from pathlib import Path

from ..core.ffmpeg_mixer import FFMpegMixer
from ..core.exceptions import FFMpegNotFoundError, MixingError


class MixerApp:
    def __init__(self) -> None:
        self.root = tk.Tk()
        self.root.title("WBand Mixer - v1")
        self.root.geometry("520x420")
        self.root.resizable(False, False)

        self.mixer = None
        try:
            self.mixer = FFMpegMixer()
        except FFMpegNotFoundError as e:
            messagebox.showerror("FFmpeg", str(e))

        self.input_listbox = tk.Listbox(self.root, selectmode=tk.SINGLE)
        self.btn_add = tk.Button(self.root, text="+", width=4, command=self.on_add)
        self.btn_remove = tk.Button(
            self.root, text="×", width=4, command=self.on_remove
        )

        self.format_var = tk.StringVar(value="wav")
        self.output_path_var = tk.StringVar(value=str(Path.home() / "output.wav"))

        self.normalize_var = tk.BooleanVar(value=False)
        self.lufs_var = tk.DoubleVar(value=-14.0)

        self.btn_browse = tk.Button(
            self.root, text="...", width=4, command=self.on_browse
        )
        self.btn_mix = tk.Button(self.root, text="MIX", height=2, command=self.on_mix)

        self.status_var = tk.StringVar(value="Готово")
        self.status_label = tk.Label(
            self.root, textvariable=self.status_var, anchor="w"
        )

        self._layout()
        self._create_format_dropdown()
        self._update_mix_state()

    def run(self) -> None:
        self.root.mainloop()

    # Layout
    def _layout(self) -> None:
        tk.Label(self.root, text="Input").place(x=12, y=10)
        self.input_listbox.place(x=12, y=30, width=440, height=220)
        self.btn_add.place(x=460, y=30, width=40, height=28)
        self.btn_remove.place(x=460, y=64, width=40, height=28)

        tk.Label(self.root, text="Output").place(x=12, y=264)
        tk.Label(self.root, text="Format:").place(x=12, y=290)

        # Normalize controls
        self.chk_normalize = tk.Checkbutton(self.root, text="Normalize", variable=self.normalize_var, command=self._sync_output_ext)
        self.chk_normalize.place(x=180, y=286, width=100, height=24)
        tk.Label(self.root, text="LUFS:").place(x=288, y=290)
        self.spin_lufs = tk.Spinbox(self.root, from_=-40.0, to=-5.0, increment=0.5, textvariable=self.lufs_var, width=6)
        self.spin_lufs.place(x=330, y=286, width=60, height=24)

        self.btn_browse.place(x=460, y=314, width=40, height=28)
        tk.Entry(self.root, textvariable=self.output_path_var).place(
            x=12, y=314, width=440, height=28
        )

        self.btn_mix.place(x=12, y=352, width=488, height=44)
        self.status_label.place(x=12, y=402, width=488, height=16)

    def _create_format_dropdown(self) -> None:
        self.format_choices = ["wav", "mp3", "flac", "ogg", "aac"]
        self.format_menu = tk.OptionMenu(
            self.root,
            self.format_var,
            *self.format_choices,
            command=lambda _: self._sync_output_ext(),
        )
        self.format_menu.place(x=70, y=284, width=100, height=28)

    # Actions
    def on_add(self) -> None:
        files = filedialog.askopenfilenames(
            title="Выберите аудио",
            filetypes=[
                ("Audio", "*.wav *.mp3 *.flac *.ogg *.aac *.m4a *.wma"),
                ("All", "*.*"),
            ],
        )
        for f in files:
            if f and (f not in self.input_listbox.get(0, tk.END)):
                self.input_listbox.insert(tk.END, f)
        self._update_mix_state()

    def on_remove(self) -> None:
        sel = self.input_listbox.curselection()
        if sel:
            self.input_listbox.delete(sel[0])
        self._update_mix_state()

    def on_browse(self) -> None:
        fmt = self.format_var.get().lower()
        def_ext = f".{fmt}"
        path = filedialog.asksaveasfilename(
            title="Выходной файл",
            defaultextension=def_ext,
            filetypes=[(fmt.upper(), f"*{def_ext}"), ("All", "*.*")],
        )
        if path:
            self.output_path_var.set(path)
        self._sync_output_ext()

    def _sync_output_ext(self) -> None:
        path = Path(self.output_path_var.get())
        fmt = self.format_var.get().lower()
        if not str(path).lower().endswith(f".{fmt}"):
            self.output_path_var.set(str(path.with_suffix(f".{fmt}")))

    def on_mix(self) -> None:
        if not self.mixer:
            messagebox.showerror("FFmpeg", "ffmpeg недоступен")
            return

        inputs = list(self.input_listbox.get(0, tk.END))
        output = self.output_path_var.get()
        fmt = self.format_var.get()
        normalize = bool(self.normalize_var.get())
        target_lufs = float(self.lufs_var.get())

        self.btn_mix.config(state=tk.DISABLED)
        self.status_var.set("Микширование...")

        def worker():
            try:
                self.mixer.mix(inputs, output, fmt, normalize=normalize, target_lufs=target_lufs)
                self.status_var.set("Готово: " + output)
                messagebox.showinfo("Микширование", "Успех")
            except (MixingError, FFMpegNotFoundError) as e:
                self.status_var.set("Ошибка")
                messagebox.showerror("Микширование", str(e))
            finally:
                self._update_mix_state()

        threading.Thread(target=worker, daemon=True).start()

    def _update_mix_state(self) -> None:
        has_inputs = self.input_listbox.size() > 0
        out_ok = bool(self.output_path_var.get())
        state = tk.NORMAL if has_inputs and out_ok else tk.DISABLED
        self.btn_mix.config(state=state)
