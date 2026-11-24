import type { FindingCode } from '~/api/types'

/**
 * The i18n key of each diagnosis code (dots become underscores: `bugcheck` and `bugcheck.none` cannot both be keys
 * of a nested message object). `satisfies` makes a new code in the API fail the typecheck until it has a text.
 */
export const findingKeys = {
  'thermal.critical': 'findings.thermal_critical',
  'thermal.at_limit': 'findings.thermal_at_limit',
  'thermal.normal': 'findings.thermal_normal',
  'rail.out_of_tolerance': 'findings.rail_out_of_tolerance',
  'rail.droop': 'findings.rail_droop',
  'rail.in_tolerance': 'findings.rail_in_tolerance',
  'load.high': 'findings.load_high',
  'load.idle': 'findings.load_idle',
  'restart.quick': 'findings.restart_quick',
  'restart.stayed_off': 'findings.restart_stayed_off',
  'power.transition': 'findings.power_transition',
  'bugcheck': 'findings.bugcheck',
  'bugcheck.none': 'findings.bugcheck_none',
  'whea.hardware_error': 'findings.whea_hardware_error',
  'whea.pcie': 'findings.whea_pcie',
  'power_button.long_press': 'findings.power_button_long_press',
  'power_button.pressed': 'findings.power_button_pressed',
  'boot.early_failure': 'findings.boot_early_failure',
  'telemetry.missing': 'findings.telemetry_missing',
  'telemetry.not_at_failure': 'findings.telemetry_not_at_failure',
  'telemetry.too_short': 'findings.telemetry_too_short',
  'estimate.lower_bound': 'findings.estimate_lower_bound',
  'sensor.unavailable': 'findings.sensor_unavailable',
  'rail.precision': 'findings.rail_precision',
  'rail.measured_on_board': 'findings.rail_measured_on_board',
  'whea.may_be_power': 'findings.whea_may_be_power',
} as const satisfies Record<FindingCode, string>
