from lib import tcx_builder

def assertTcxSportMatches(targetSport, tcx):
    assert tcx[0][0].attrib["Sport"] == targetSport

def assertTcxIdMatches(workout, tcx):
    assert tcx[0][0][0].text == tcx_builder.getTimeStamp(workout['start_time'])

def assertTcxLapStartTimeMatches(workout, tcx):
    assert tcx[0][0][2].attrib["StartTime"] == tcx_builder.getTimeStamp(workout['start_time'])

def assertTcxTotalTimeSecondsMatches(workout, tcx):
    actual = tcx[0][0][2][0].text 
    expected = str(workout["ride"]["duration"])
    assert actual == expected

def assertTcxMaximumSpeedMatches(workoutSamples, tcx):
    actual = tcx[0][0][2][2].text
    _, originalDistanceUnit = tcx_builder.getDistanceMeters(workoutSamples) 
    expected = str(tcx_builder.getMaxSpeedMetersPerSecond(workoutSamples, originalDistanceUnit))
    assert actual == expected

def assertTcxCaloriesMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][5].text 
    expected = str(int(round((workoutSummary["calories"]))))
    assert actual == expected

def assertTcxAvgHeartRateMatches(workoutSummary,tcx):
    actual = tcx[0][0][2][3][0].text 
    expected = tcx_builder.getHeartRate(workoutSummary["avg_heart_rate"])
    assert actual == expected

def assertTcxMaxHeartRateMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][4][0].text 
    expected = tcx_builder.getHeartRate(workoutSummary["max_heart_rate"])
    assert actual == expected

def assertTcxMaxBikeCadenceMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][2].text 
    expected = tcx_builder.getCadence(workoutSummary["max_cadence"])
    assert actual == expected

def assertTcxAvgWattsMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][5].text 
    expected = "{0:.0f}".format(workoutSummary["avg_power"])
    assert actual == expected

def assertTcxMaxWattsMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][6].text 
    expected = "{0:.0f}".format(workoutSummary["max_power"])
    assert actual == expected

def assertTcxTotalPowerMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][0].text 
    expected = "{0:.0f}".format(workoutSummary["total_work"])
    assert actual == expected

def assertTcxAvgBikeCadenceMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][1].text 
    expected = "{0:.0f}".format(workoutSummary["avg_cadence"])
    assert actual == expected

def assertTcxAvgResistanceMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][3].text 
    expected = "{0:.0f}".format(workoutSummary["avg_resistance"])
    assert actual == expected

def assertTcxMaxResistanceMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][8][0][4].text 
    expected = "{0:.0f}".format(workoutSummary["max_resistance"])
    assert actual == expected

def assertTcxIntensityMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][6].text 
    expected = "Active"
    assert actual == expected

def assertTcxTriggerMethodMatches(workoutSummary, tcx):
    actual = tcx[0][0][2][7].text 
    expected = "Manual"
    assert actual == expected