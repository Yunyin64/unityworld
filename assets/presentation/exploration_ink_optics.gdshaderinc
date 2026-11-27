// Adapted from Tideborn's src/ink/shared-ink/ink-wash.js (MIT).
// Color and coverage can be composited without flattening dry gaps into grey.
float tideborn_ink_coverage(float pigment, float reserve, float deposit, float lift) {
    float reflectance = exp(-clamp(pigment, 0.0, 1.6) * 3.1);
    float optical = -log(max(reflectance, 0.005));
    optical = max(0.0, optical + deposit * 0.9 - lift * 1.4);
    return (1.0 - exp(-optical)) * (1.0 - clamp(reserve, 0.0, 1.0));
}
