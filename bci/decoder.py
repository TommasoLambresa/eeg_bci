"""Decoders: scikit-learn pipelines from EEG windows to class probabilities, with training and loading."""

import pickle
from pathlib import Path

import numpy as np
from pyriemann.estimation import Covariances
from pyriemann.tangentspace import TangentSpace
from sklearn.base import clone
from sklearn.discriminant_analysis import LinearDiscriminantAnalysis
from sklearn.model_selection import StratifiedGroupKFold, cross_val_predict
from sklearn.pipeline import make_pipeline

# each decoder maps windows (windows x channels x samples) to class probabilities; add new ones here
DECODERS = {
    # covariance of each window, tangent space at the Riemannian mean, shrinkage LDA
    "tangent_space_lda": make_pipeline(
        Covariances(estimator="oas"),
        TangentSpace(metric="riemann"),
        LinearDiscriminantAnalysis(solver="lsqr", shrinkage="auto"),
    ),
}


def train_decoder(decoder_name: str, windows: np.ndarray, labels: np.ndarray, trials: np.ndarray,
                  fold_count: int, model_path: str | Path, settings: dict) -> np.ndarray:
    """Cross-validate a decoder, fit it on all windows and save it with its settings.

    Windows of one trial share a fold. Returns the cross-validated prediction of each window.
    """
    folds = StratifiedGroupKFold(n_splits=fold_count, shuffle=True, random_state=0)
    predictions = cross_val_predict(DECODERS[decoder_name], windows, labels, groups=trials, cv=folds)
    pipeline = clone(DECODERS[decoder_name]).fit(windows, labels)
    Path(model_path).parent.mkdir(parents=True, exist_ok=True)
    with open(model_path, "wb") as model_file:
        pickle.dump({"pipeline": pipeline, "decoder_name": decoder_name, **settings}, model_file)
    return predictions


def load_model(model_path: str | Path) -> dict:
    """Return the saved model: the trained pipeline under "pipeline", plus its settings."""
    with open(model_path, "rb") as model_file:
        return pickle.load(model_file)