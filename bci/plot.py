"""Offline plots of saved blocks."""

import matplotlib.pyplot as plt
import numpy as np
from sklearn.metrics import ConfusionMatrixDisplay


def plot_erd(erd_percent: np.ndarray, labels: np.ndarray) -> None:
    """Mean desynchronization of each class across channels; the dashed line splits the two channel halves."""
    channel_numbers = np.arange(1, erd_percent.shape[1] + 1)
    for class_name in np.unique(labels):
        plt.plot(channel_numbers, erd_percent[labels == class_name].mean(axis=0), marker="o", label=class_name)
    plt.axvline(erd_percent.shape[1] / 2 + 0.5, color="gray", linestyle="--")
    plt.axhline(0, color="black", linewidth=0.5)
    plt.xlabel("channel")
    plt.ylabel("mu-band power change during imagery (%)")
    plt.legend()
    plt.show()


def plot_confusion_matrix(true_labels: np.ndarray, predicted_labels: np.ndarray) -> None:
    """Fraction of the windows of each true class assigned to each predicted class."""
    ConfusionMatrixDisplay.from_predictions(true_labels, predicted_labels, normalize="true")
    plt.show()
