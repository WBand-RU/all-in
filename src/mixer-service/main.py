from dotenv import load_dotenv
from app.worker import MixWorker

load_dotenv()

if __name__ == "__main__":
    worker = MixWorker()
    worker.run()
