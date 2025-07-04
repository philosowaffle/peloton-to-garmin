# Troubleshooting Guide

## Common Issues and Solutions

### Authentication Issues

#### Peloton Authentication Failures
**Symptoms:**
- "Failed to authenticate with Peloton" error
- HTTP 401 Unauthorized responses
- Session timeout errors

**Solutions:**
1. **Verify Credentials:**
   ```json
   {
     "Peloton": {
       "Email": "correct-email@example.com",
       "Password": "correct-password"
     }
   }
   ```

2. **Check Account Status:**
   - Ensure Peloton account is active
   - Verify email/password by logging into Peloton website
   - Check for account lockouts

3. **Clear Cached Authentication:**
   - Delete authentication cache files
   - Restart application to force re-authentication

4. **Network Connectivity:**
   - Test connection to `api.onepeloton.com`
   - Check firewall/proxy settings
   - Verify DNS resolution

**Debug Steps:**
```bash
# Test Peloton API connectivity
curl -v https://api.onepeloton.com/api/me

# Check logs for authentication details
grep -i "peloton.*auth" logs/p2g_console_log.txt
```

#### Garmin Authentication Failures
**Symptoms:**
- "Failed to authenticate with Garmin" error
- MFA code required but not provided
- OAuth token exchange failures

**Solutions:**
1. **Configure MFA Settings:**
   ```json
   {
     "Garmin": {
       "Email": "correct-email@example.com",
       "Password": "correct-password",
       "TwoStepVerificationEnabled": true
     }
   }
   ```

2. **MFA Code Issues:**
   - Ensure MFA is properly configured in Garmin Connect
   - Use authenticator app, not SMS when possible
   - Check system time synchronization

3. **OAuth Token Issues:**
   - Clear stored OAuth tokens
   - Restart authentication flow
   - Check Garmin Connect API status

**Debug Steps:**
```bash
# Check Garmin Connect connectivity
curl -v https://connect.garmin.com

# View OAuth flow logs
grep -i "oauth\|garmin.*auth" logs/p2g_console_log.txt
```

### Sync Process Issues

#### No Workouts Found
**Symptoms:**
- "No workouts to sync" message
- Empty workout lists
- Sync completes but no files generated

**Solutions:**
1. **Check Date Range:**
   - Verify `NumWorkoutsToDownload` setting
   - Ensure recent workouts exist on Peloton
   - Check workout completion status

2. **Workout Filtering:**
   ```json
   {
     "Peloton": {
       "ExcludeWorkoutTypes": []
     }
   }
   ```

3. **Account Permissions:**
   - Verify Peloton account has workout data
   - Check privacy settings on Peloton account

#### Conversion Failures
**Symptoms:**
- "All configured converters failed" error
- Missing output files
- Conversion timeout errors

**Solutions:**
1. **Check Output Formats:**
   ```json
   {
     "Format": {
       "Fit": true,
       "Json": false,
       "Tcx": false
     }
   }
   ```

2. **File Permissions:**
   - Ensure write permissions to output directories
   - Check disk space availability
   - Verify antivirus software isn't blocking file creation

3. **Workout Data Issues:**
   - Check for corrupted workout data
   - Verify workout has required metrics
   - Test with different workout types

#### Upload Failures
**Symptoms:**
- "Failed to upload to Garmin Connect" error
- Files created but not uploaded
- Duplicate activity errors

**Solutions:**
1. **Check Upload Settings:**
   ```json
   {
     "Garmin": {
       "Upload": true,
       "FormatToUpload": "fit"
     }
   }
   ```

2. **File Format Issues:**
   - Ensure FIT or TCX format is enabled
   - Verify file integrity
   - Check file size limits

3. **Garmin Connect Issues:**
   - Check Garmin Connect service status
   - Verify account has upload permissions
   - Test manual upload on Garmin Connect website

### Configuration Issues

#### Invalid Configuration
**Symptoms:**
- "Configuration validation failed" error
- Application won't start
- Missing configuration sections

**Solutions:**
1. **Validate JSON Syntax:**
   ```bash
   # Validate JSON format
   python -m json.tool configuration.local.json
   ```

2. **Check Required Fields:**
   - Ensure all required configuration sections exist
   - Verify data types match expected values
   - Check for missing quotes or commas

3. **Reset to Defaults:**
   ```bash
   # Backup current config
   cp configuration.local.json configuration.backup.json
   
   # Copy from example
   cp configuration.example.json configuration.local.json
   ```

#### Environment Variable Issues
**Symptoms:**
- Configuration not loading from environment
- Partial configuration applied
- Environment variable naming errors

**Solutions:**
1. **Check Variable Names:**
   ```bash
   # Correct format
   P2G_Peloton__Email=user@example.com
   P2G_Peloton__Password=password123
   
   # Incorrect format
   P2G_PELOTON_EMAIL=user@example.com  # Wrong separator
   ```

2. **Verify Variable Loading:**
   ```bash
   # Windows
   echo %P2G_Peloton__Email%
   
   # Linux/Mac
   echo $P2G_Peloton__Email
   ```

### Performance Issues

#### Slow Sync Performance
**Symptoms:**
- Sync takes longer than expected
- High CPU/memory usage
- Timeout errors

**Solutions:**
1. **Optimize Batch Size:**
   ```json
   {
     "Peloton": {
       "NumWorkoutsToDownload": 5
     }
   }
   ```

2. **Check System Resources:**
   - Monitor CPU and memory usage
   - Ensure sufficient disk space
   - Close unnecessary applications

3. **Network Optimization:**
   - Check internet connection speed
   - Verify API response times
   - Consider rate limiting issues

#### Memory Issues
**Symptoms:**
- Out of memory exceptions
- Application crashes during sync
- High memory usage

**Solutions:**
1. **Reduce Batch Size:**
   - Process fewer workouts at once
   - Enable file cleanup after processing

2. **Check for Memory Leaks:**
   - Monitor memory usage over time
   - Restart application periodically
   - Update to latest version

### File System Issues

#### Permission Errors
**Symptoms:**
- "Access denied" errors
- Cannot create output files
- Database write failures

**Solutions:**
1. **Check Directory Permissions:**
   ```bash
   # Windows
   icacls "C:\Users\%USERNAME%\AppData\Roaming\P2G" /grant %USERNAME%:F
   
   # Linux/Mac
   chmod 755 ~/.local/share/P2G
   ```

2. **Run as Administrator:**
   - Try running application with elevated permissions
   - Check if antivirus is blocking file operations

#### Disk Space Issues
**Symptoms:**
- "Insufficient disk space" errors
- File creation failures
- Database corruption

**Solutions:**
1. **Check Available Space:**
   ```bash
   # Windows
   dir C:\ /-c
   
   # Linux/Mac
   df -h
   ```

2. **Clean Up Old Files:**
   - Remove old workout files
   - Clean up log files
   - Compact database files

### Database Issues

#### Database Corruption
**Symptoms:**
- SQLite database errors
- Settings not persisting
- Sync status not updating

**Solutions:**
1. **Check Database Integrity:**
   ```bash
   sqlite3 ~/.local/share/P2G/settings.db "PRAGMA integrity_check;"
   ```

2. **Repair Database:**
   ```bash
   # Backup database
   cp settings.db settings.backup.db
   
   # Attempt repair
   sqlite3 settings.db ".recover" | sqlite3 settings_recovered.db
   ```

3. **Reset Database:**
   ```bash
   # Remove corrupted database (will recreate on next run)
   rm ~/.local/share/P2G/settings.db
   ```

### Network Issues

#### Connectivity Problems
**Symptoms:**
- "Network unreachable" errors
- Timeout exceptions
- DNS resolution failures

**Solutions:**
1. **Test Connectivity:**
   ```bash
   # Test Peloton API
   curl -v https://api.onepeloton.com/api/me
   
   # Test Garmin Connect
   curl -v https://connect.garmin.com
   ```

2. **Check Proxy Settings:**
   - Configure proxy in application settings
   - Test direct connection without proxy
   - Verify proxy authentication

3. **Firewall Configuration:**
   - Allow application through firewall
   - Check corporate firewall rules
   - Verify required ports are open

### Docker Issues

#### Container Startup Failures
**Symptoms:**
- Container exits immediately
- "No such file or directory" errors
- Permission denied in container

**Solutions:**
1. **Check Volume Mounts:**
   ```bash
   # Verify volume mount paths
   docker run -v /host/path:/container/path p2g-console
   ```

2. **File Permissions:**
   ```bash
   # Fix permissions on host
   chmod 755 /host/config/path
   chown -R 1000:1000 /host/config/path
   ```

3. **Container Logs:**
   ```bash
   # View container logs
   docker logs container-name
   
   # Follow logs in real-time
   docker logs -f container-name
   ```

## Diagnostic Tools

### Log Analysis
```bash
# View recent logs
tail -f logs/p2g_console_log.txt

# Search for specific errors
grep -i "error\|exception\|fail" logs/p2g_console_log.txt

# View authentication logs
grep -i "auth" logs/p2g_console_log.txt
```

### Configuration Validation
```bash
# Validate JSON configuration
python -c "import json; json.load(open('configuration.local.json'))"

# Check environment variables
env | grep P2G_
```

### Network Diagnostics
```bash
# Test API connectivity
curl -v https://api.onepeloton.com/api/me
curl -v https://connect.garmin.com

# Check DNS resolution
nslookup api.onepeloton.com
nslookup connect.garmin.com
```

### Database Diagnostics
```bash
# Check database integrity
sqlite3 settings.db "PRAGMA integrity_check;"

# View table structure
sqlite3 settings.db ".schema"

# Check database size
ls -lh *.db
```

## Getting Help

### Before Reporting Issues
1. **Check Logs:** Review application logs for error details
2. **Test Connectivity:** Verify network access to APIs
3. **Validate Configuration:** Ensure configuration is correct
4. **Try Latest Version:** Update to the latest release

### Information to Include
- **Version:** Application version number
- **Platform:** Operating system and version
- **Configuration:** Sanitized configuration (remove credentials)
- **Logs:** Relevant log entries
- **Steps to Reproduce:** Detailed reproduction steps

### Support Channels
- **GitHub Issues:** [Report bugs and feature requests](https://github.com/philosowaffle/peloton-to-garmin/issues)
- **GitHub Discussions:** [Community support](https://github.com/philosowaffle/peloton-to-garmin/discussions)
- **Documentation:** [Project wiki](https://philosowaffle.github.io/peloton-to-garmin)

## Prevention Tips

### Regular Maintenance
- **Update Regularly:** Keep application updated
- **Monitor Logs:** Check logs for warnings
- **Backup Configuration:** Save configuration files
- **Test Periodically:** Verify sync functionality

### Best Practices
- **Use Strong Passwords:** Secure account credentials
- **Monitor API Changes:** Watch for Peloton/Garmin API updates
- **Resource Management:** Monitor disk space and memory usage
- **Error Handling:** Implement proper error handling in custom integrations 