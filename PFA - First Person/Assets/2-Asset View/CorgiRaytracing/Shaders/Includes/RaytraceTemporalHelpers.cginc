
int TemporallyRendered;
int TemporalPassIndex;
int TemporalPassCount;

#define TEMPORAL_SKIP(id) if (TemporallyRendered > 0) { if (id.x % TemporalPassCount != TemporalPassIndex && id.y % TemporalPassCount != TemporalPassIndex) return; }