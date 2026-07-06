from flask import Flask
from threading import Thread

app = Flask("")


@app.route("/")
def home():
    return "Server is Running!"


def run():
    app.run(host="0.0.0.0", port=8000)


if __name__ == "__main__":
    t = Thread(target=run)
    t.start()
