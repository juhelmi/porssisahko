*** Settings ***
Library    RPA.Windows
Library    Process
Library    PriceFixture.py
Suite Setup    Initialize Fixture
Suite Teardown    Delete Price Fixture
Test Teardown    Run Keyword And Ignore Error    Terminate Process

*** Variables ***
${APP_TITLE}    Laske Edullisin Lataus

*** Test Cases ***
Calculate Using Preloaded Price JSON
    Start Process    dotnet    run    --project    ${PROJECT_FILE}    --no-restore
    Wait Until Keyword Succeeds    45 sec    1 sec    Control Window    name:"${APP_TITLE}"
    Set Value    id:PriceFilePath depth:16    ${PRICE_FILE}
    Set Value    id:CurrentSocTextBox depth:16    40
    Set Value    id:TargetSocTextBox depth:16    41
    Set Value    id:BatteryCapacityKwhTextBox depth:16    1
    Set Value    id:ChargeStartTimeTextBox depth:16    00:00
    Set Value    id:ChargeEndTimeTextBox depth:16    00:00
    Set Value    id:TotalPowerKwTextBox depth:16    5.52
    Set Value    id:BaseLossWattsTextBox depth:16    300
    Set Value    id:CurrentDependentFactorTextBox depth:16    0.055
    ${price_file}=    Get Value    id:PriceFilePath depth:16
    Should Be Equal As Strings    ${price_file}    ${PRICE_FILE}
    ${current_soc}=    Get Value    id:CurrentSocTextBox depth:16
    Should Be Equal As Strings    ${current_soc}    40
    Click    id:CalculateButton depth:16
    Wait Until Keyword Succeeds    30 sec    1 sec    Calculation Should Complete

*** Keywords ***
Initialize Fixture
    ${path}=    Create Price Fixture
    Set Suite Variable    ${PRICE_FILE}    ${path}

Calculation Should Complete
    ${status}=    Get Attribute    id:StatusMessage depth:16    Name
    Should Contain    ${status}    Calculation completed
    Should Contain    ${status}    from the local JSON file
    ${summary}=    Get Attribute    id:ResultSummary depth:16    Name
    Should Contain    ${summary}    Total cost: