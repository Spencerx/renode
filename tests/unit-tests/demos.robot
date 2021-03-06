*** Settings ***
Suite Setup                   Get Test Cases
Suite Teardown                Teardown
Test Setup                    Reset Emulation
Resource                      ${CURDIR}/../../src/Renode/RobotFrameworkEngine/renode-keywords.robot

*** Variables ***
@{scripts_path}=              ${CURDIR}/../../scripts
@{pattern}=                   *.resc

*** Keywords ***
Get Test Cases
    Setup
    @{scripts}=  List Files In Directory Recursively  @{scripts_path}  @{pattern}
    Set Suite Variable  @{scripts}

Load Script
    [Arguments]               ${path}
    Execute Script            ${path}

*** Test Cases ***
Should Load Demos
    :FOR  ${script}  IN  @{scripts}
    \	  Load Script  ${script}
    \     Reset Emulation 
	

