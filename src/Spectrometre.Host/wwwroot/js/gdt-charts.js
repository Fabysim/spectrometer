(function () {
    function hexToRgba(hex, alpha) {
        if (!hex) return 'rgba(100,116,139,' + alpha + ')';
        var h = hex.replace('#', '').trim();
        if (h.length === 3) {
            h = h.split('').map(function (c) { return c + c; }).join('');
        }
        var r = parseInt(h.substring(0, 2), 16);
        var g = parseInt(h.substring(2, 4), 16);
        var b = parseInt(h.substring(4, 6), 16);
        return 'rgba(' + r + ',' + g + ',' + b + ',' + alpha + ')';
    }

    function formatHours(v) {
        if (v == null || isNaN(v)) return '0h';
        return (Math.round(v * 10) / 10) + 'h';
    }

    function formatAxisHours(v) {
        if (v == null || isNaN(v)) return '0h';
        if (v >= 100) return Math.round(v) + 'h';
        return v + 'h';
    }

    window.gdtCharts = {
        _bar: null,
        _radar: null,
        _generation: 0,

        destroy: function () {
            if (this._bar) {
                this._bar.destroy();
                this._bar = null;
            }
            if (this._radar) {
                this._radar.destroy();
                this._radar = null;
            }
        },

        /** Ne détruit que si la génération correspond — protège contre le Dispose async d'une page précédente. */
        destroyIf: function (generation) {
            if (generation == null || this._generation !== generation)
                return;
            this.destroy();
        },

        initBarChart: function (canvasId, labels, allocData, usedData, colors, generation) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || typeof Chart === 'undefined') return;

            if (generation != null)
                this._generation = generation;

            var ctx = canvas.getContext('2d');
            if (this._bar) {
                this._bar.destroy();
            }

            this._bar = new Chart(ctx, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'Théorique',
                            data: allocData,
                            backgroundColor: colors.map(function (c) { return hexToRgba(c, 0.27); }),
                            borderColor: colors.map(function (c) { return hexToRgba(c, 0.55); }),
                            borderWidth: 1,
                            borderRadius: 4
                        },
                        {
                            label: 'Réel',
                            data: usedData,
                            backgroundColor: colors.map(function (c) { return hexToRgba(c, 0.8); }),
                            borderColor: colors,
                            borderWidth: 1,
                            borderRadius: 4
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    return ctx.dataset.label + ' · ' + formatHours(ctx.parsed.y);
                                }
                            }
                        }
                    },
                    scales: {
                        y: {
                            beginAtZero: true,
                            ticks: {
                                callback: function (v) { return formatAxisHours(v); }
                            }
                        }
                    }
                }
            });
        },

        initRadarChart: function (canvasId, labels, allocData, usedData, axisColors, generation) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || typeof Chart === 'undefined') return;

            if (generation != null)
                this._generation = generation;

            var ctx = canvas.getContext('2d');
            if (this._radar) {
                this._radar.destroy();
            }

            this._radar = new Chart(ctx, {
                type: 'radar',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'Théorique',
                            data: allocData,
                            borderColor: 'rgba(75,106,118,0.8)',
                            backgroundColor: 'rgba(75,106,118,0.15)',
                            borderWidth: 2,
                            pointRadius: 3
                        },
                        {
                            label: 'Réel',
                            data: usedData,
                            borderColor: 'rgba(122,63,45,0.85)',
                            backgroundColor: 'rgba(122,63,45,0.1)',
                            borderWidth: 2,
                            borderDash: [6, 4],
                            pointRadius: 3
                        }
                    ]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    var i = ctx.dataIndex;
                                    var alloc = allocData[i] || 0;
                                    var used = usedData[i] || 0;
                                    var diff = used - alloc;
                                    var sign = diff >= 0 ? '+' : '';
                                    return ctx.dataset.label + ' · ' + formatHours(used) + ' (' + sign + formatHours(diff) + ')';
                                }
                            }
                        }
                    },
                    scales: {
                        r: {
                            beginAtZero: true,
                            ticks: {
                                callback: function (v) { return formatAxisHours(v); }
                            },
                            pointLabels: {
                                color: axisColors,
                                font: { size: 11, weight: '600' }
                            }
                        }
                    }
                }
            });
        },

        /** Barres horizontales compactes pour le dashboard owner (canvas indépendant). */
        initOwnerDashboardBarChart: function (canvasId, labels, allocData, usedData, colors) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || typeof Chart === 'undefined') return false;

            var existing = Chart.getChart(canvas);
            if (existing) existing.destroy();

            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [
                        {
                            label: 'Théorique',
                            data: allocData,
                            backgroundColor: colors.map(function (c) { return hexToRgba(c, 0.2); }),
                            borderRadius: 3,
                            borderSkipped: false
                        },
                        {
                            label: 'Réel',
                            data: usedData,
                            backgroundColor: colors.map(function (c) { return hexToRgba(c, 0.8); }),
                            borderRadius: 3,
                            borderSkipped: false
                        }
                    ]
                },
                options: {
                    indexAxis: 'y',
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: {
                        legend: { display: false },
                        tooltip: {
                            callbacks: {
                                label: function (ctx) {
                                    return ctx.dataset.label + ' · ' + formatHours(ctx.parsed.x);
                                }
                            }
                        }
                    },
                    scales: {
                        x: {
                            beginAtZero: true,
                            grid: { color: 'rgba(0,0,0,0.04)' },
                            ticks: {
                                callback: function (v) { return v + 'h'; },
                                font: { size: 11 }
                            },
                            border: { display: false }
                        },
                        y: {
                            grid: { display: false },
                            ticks: { font: { size: 11 } },
                            border: { display: false }
                        }
                    }
                }
            });
            return true;
        },

        /** KPIE par service — dashboard owner. */
        initServiceKpieChart: function (canvasId, labels, values, datasetLabel) {
            var canvas = document.getElementById(canvasId);
            if (!canvas || typeof Chart === 'undefined') return false;

            var existing = Chart.getChart(canvas);
            if (existing) existing.destroy();

            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: datasetLabel || 'KPIE (%)',
                        data: values,
                        backgroundColor: 'rgba(52, 95, 109, 0.8)',
                        borderRadius: 4
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    plugins: { legend: { display: false } },
                    scales: {
                        y: {
                            beginAtZero: true,
                            max: 100,
                            ticks: {
                                callback: function (v) { return v + '%'; }
                            }
                        }
                    }
                }
            });
            return true;
        },

        updateCharts: function (allocData, usedData, axisColors) {
            if (this._bar) {
                this._bar.data.datasets[0].data = allocData;
                this._bar.data.datasets[1].data = usedData;
                this._bar.update('none');
            }
            if (this._radar) {
                this._radar.data.datasets[0].data = allocData;
                this._radar.data.datasets[1].data = usedData;
                if (this._radar.options.scales.r.pointLabels) {
                    this._radar.options.scales.r.pointLabels.color = axisColors;
                }
                this._radar.update('none');
            }
        }
    };
})();

