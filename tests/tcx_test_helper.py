from lib import tcx_builder

def assertTcxSportMatches(targetSport, tcx):
    assert tcx[0][0].attrib["Sport"] == targetSport

def assertTcxIdMatches(workout, tcx):
    assert tcx[0][0][0].text == tcx_builder.getTimeStamp(workout['start_time'])

def assertTcxLapStartTimeMatches(workout, tcx):
    assert tcx[0][0][1].attrib["StartTime"] == tcx_builder.getTimeStamp(workout['start_time'])

def assertTcxTotalTimeSecondsMatches(workout, tcx):
    actual = tcx[0][0][1][0].text 
    expected = str(workout["ride"]["duration"])
    assert actual == expected

def assertTcxMaximumSpeedMatches(workoutSamples, tcx):
    actual = tcx[0][0][1][2].text
    _, originalDistanceUnit = tcx_builder.getDistanceMeters(workoutSamples) 
    expected = str(tcx_builder.getMaxSpeedMetersPerSecond(workoutSamples, originalDistanceUnit))
    assert actual == expected

def assertTcxCaloriesMatches(workoutSummary, tcx):
    actual = tcx[0][0][1][3].text 
    expected = str(int(round((workoutSummary["calories"]))))
    assert actual == expected

def assertTcxAvgHeartRateMatches(workoutSummary,tcx):
    actual = tcx[0][0][1][4][0].text 
    expected = tcx_builder.getHeartRate(workoutSummary["avg_heart_rate"])
    assert actual == expected

def assertTcxMaxHeartRateMatches(workoutSummary, tcx):
    actual = tcx[0][0][1][5][0].text 
    expected = tcx_builder.getHeartRate(workoutSummary["max_heart_rate"])
    assert actual == expected

def assertTcxAvgSpeedMatches(workoutSamples, tcx):
    actual = tcx[0][0][1][7][0][0].text 
    _, originalDistanceUnit = tcx_builder.getDistanceMeters(workoutSamples) 
    expected = str(tcx_builder.getAverageSpeedMetersPerSecond(workoutSamples, originalDistanceUnit))
    assert actual == expected

def assertTcxMaxBikeCadenceMatches(workoutSummary, tcx):
    actual = tcx[0][0][1][7][0][1].text 
    expected = tcx_builder.getCadence(workoutSummary["max_cadence"])
    assert actual == expected

def assertTcxAvgWattsMatches(workoutSummary, tcx):
    actual = tcx[0][0][1][7][0][2].text 
    expected = "{0:.0f}".format(workoutSummary["avg_power"])
    assert actual == expected

def assertTcxMaxWattsMatches(workoutSummary, tcx):
    actual = tcx[0][0][1][7][0][3].text 
    expected = "{0:.0f}".format(workoutSummary["max_power"])
    assert actual == expected
