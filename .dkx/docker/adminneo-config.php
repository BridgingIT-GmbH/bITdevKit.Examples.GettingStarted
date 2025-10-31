<?php

$variables = [
  "theme" => "NEO_THEME",
  "colorVariant" => "NEO_COLOR_VARIANT",
  "cssUrls" => "NEO_CSS_URLS",
  "jsUrls" => "NEO_JS_URLS",
  "navigationMode" => "NEO_NAVIGATION_MODE",
  "preferSelection" => "NEO_PREFER_SELECTION",
  "jsonValuesDetection" => "NEO_JSON_VALUES_DETECTION",
  "jsonValuesAutoFormat" => "NEO_JSON_VALUES_AUTO_FORMAT",
  "enumAsSelectThreshold" => "NEO_ENUM_AS_SELECT_THRESHOLD",
  "recordsPerPage" => "NEO_RECORDS_PER_PAGE",
  "versionVerification" => "NEO_VERSION_VERIFICATION",
  "hiddenDatabases" => "NEO_HIDDEN_DATABASES",
  "hiddenSchemas" => "NEO_HIDDEN_SCHEMAS",
  "visibleCollations" => "NEO_VISIBLE_COLLATIONS",
  "defaultDriver" => "NEO_DEFAULT_DRIVER",
  "defaultServer" => "NEO_DEFAULT_SERVER",
  "defaultDatabase" => "NEO_DEFAULT_DATABASE",
  "defaultPasswordHash" => "NEO_DEFAULT_PASSWORD_HASH",
  "sslKey" => "NEO_SSL_KEY",
  "sslCertificate" => "NEO_SSL_CERTIFICATE",
  "sslCaCertificate" => "NEO_SSL_CA_CERTIFICATE",
  "sslTrustServerCertificate" => "NEO_SSL_TRUST_SERVER_CERTIFICATE",
  "sslMode" => "NEO_SSL_MODE",
  "sslEncrypt" => "NEO_SSL_ENCRYPT",
];

$config = [];

// apply env overrides
foreach ($variables as $option => $envName) {
  if (!is_string($envName) || $envName === '') {
    continue;
  }

  $value = getenv($envName);

  if ($value === false) {
    continue;
  } elseif ($value === "null") {
    $value = null;
  } elseif ($value === "true") {
    $value = true;
  } elseif ($value === "false") {
    $value = false;
  } elseif (is_numeric($value)) {
    $value = (int) $value;
  }

  $config[$option] = $value;
}

$serversJson = getenv('NEO_SERVERS');
if ($serversJson !== false) {
  $servers = json_decode($serversJson, true);
  if (json_last_error() === JSON_ERROR_NONE && is_array($servers)) {
    $config['servers'] = $servers;
  } else {
    error_log('Invalid NEO_SERVERS JSON: ' . json_last_error_msg());
  }
}

return $config;