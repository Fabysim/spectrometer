(function () {
    window.grilleObservationChart = {
        _line: null,
        _generation: 0,

        destroy: function () {
            if (this._line) {
                this._line.destroy();
                this._line = null;
            }
        },

        /** Ne détruit que si la génération correspond — protège contre le Dispose async d'une page précédente. */
        destroyIf: function (generation) {
            if (generation == null || this._generation !== generation)
                return;
            this.destroy();
        },

        initLigneChart: function (canvasId, labels, moyennes, generation) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || typeof Chart === 'undefined')
                return;

            if (generation != null)
                this._generation = generation;

            var ctx = canvas.getContext('2d');
            if (this._line) {
                this._line.destroy();
                this._line = null;
            }

            var primary = 'rgb(52, 95, 109)';
            var fill = 'rgba(52, 95, 109, 0.12)';

            this._line = new Chart(ctx, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        data: moyennes,
                        borderColor: primary,
                        backgroundColor: fill,
                        borderWidth: 2,
                        pointBackgroundColor: primary,
                        pointBorderColor: '#fff',
                        pointBorderWidth: 1.5,
                        pointRadius: 4,
                        pointHoverRadius: 5,
                        fill: true,
                        tension: 0.25,
                        spanGaps: true
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function (item) {
                                    if (item.parsed.y == null)
                                        return '';
                                    return item.parsed.y.toFixed(1) + ' / 5';
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            min: 1,
                            max: 5,
                            ticks: {
                                stepSize: 1
                            }
                        }
                    }
                }
            });
        }
    };
})();
